#!/usr/bin/env python3.11
"""
mac_recorder.py
---------------
Runs on Mac alongside Unity.
Handles: recording participant speech, transcription via Whisper.

The robot_listener.py runs on the robot and handles speech + movements.
This script runs independently on your Mac during the same session.

Usage:
    python3.11 mac_recorder.py

Enter participant ID when prompted.
Press Ctrl+C at end of session to save transcript and audio.

Output saved to: ~/HRI_Study_Data/session_[timestamp]/
- segment_001.wav, segment_002.wav ... (individual speech segments)
- transcript.txt (full timestamped transcript)
"""

import threading
import time
import datetime
import os
import sys

import numpy as np
import sounddevice as sd
import whisper
from scipy.io.wavfile import write as wav_write

# ── Configuration ─────────────────────────────────────────────────────────────

SAMPLE_RATE     = 16000   # Hz — required by Whisper
CHANNELS        = 1
VAD_THRESHOLD   = 0.02    # volume threshold — increase if false positives
VAD_SILENCE_SEC = 1.5     # seconds of silence to end a segment
MIN_SPEECH_SEC  = 0.5     # minimum duration to transcribe
WHISPER_MODEL   = "base"  # tiny / base / small — base recommended
OUTPUT_DIR      = os.path.expanduser("~/HRI_Study_Data")

# ── Setup ─────────────────────────────────────────────────────────────────────

def setup_session():
    participant_id = input("Enter participant ID (e.g. P001): ").strip()
    if not participant_id:
        participant_id = "unknown"

    os.makedirs(OUTPUT_DIR, exist_ok=True)
    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    session_dir = os.path.join(OUTPUT_DIR, f"{participant_id}_{timestamp}")
    os.makedirs(session_dir, exist_ok=True)

    print(f"\n[SETUP] Participant: {participant_id}")
    print(f"[SETUP] Session data → {session_dir}\n")
    return participant_id, session_dir

# ── Save session ──────────────────────────────────────────────────────────────

def save_session(session_dir, transcript, segment_paths):
    if not transcript:
        print("\n[SAVE] No transcript to save.")
        return

    path = os.path.join(session_dir, "transcript.txt")
    with open(path, 'w') as f:
        f.write("Culture in HRI Study — Participant Transcript\n")
        f.write(f"Session: {os.path.basename(session_dir)}\n")
        f.write("=" * 50 + "\n\n")
        for entry in transcript:
            f.write(entry + "\n")

    print(f"\n[SAVE] Transcript → {path}")
    print(f"[SAVE] {len(segment_paths)} audio segments saved.")

# ── Transcription ─────────────────────────────────────────────────────────────

def transcribe_segment(model, audio, session_dir, index, transcript, lock):
    """Transcribes one speech segment and appends to transcript."""
    wav_path = os.path.join(session_dir, f"segment_{index:03d}.wav")
    wav_write(wav_path, SAMPLE_RATE, (audio * 32767).astype(np.int16))

    print(f"[TRANSCRIBE] Transcribing segment {index}...")

    try:
        result = model.transcribe(wav_path, language='en')
        text = result['text'].strip()

        if text:
            timestamp = datetime.datetime.now().strftime("%H:%M:%S")
            entry = f"[{timestamp}] PARTICIPANT: {text}"
            with lock:
                transcript.append(entry)
            print(f"[TRANSCRIBE] {entry}")
        else:
            print(f"[TRANSCRIBE] Segment {index}: no speech detected.")

    except Exception as e:
        print(f"[TRANSCRIBE] Error on segment {index}: {e}")

    return wav_path

# ── Main recording loop ───────────────────────────────────────────────────────

def record_loop(model, session_dir, stop_event):
    """
    Monitors microphone continuously.
    When participant speech detected:
      - Records segment
      - Transcribes when speech ends
    """
    transcript  = []
    segments    = []
    lock        = threading.Lock()

    audio_buffer   = []
    recording      = False
    silence_frames = 0
    segment_index  = 0

    chunk_duration = 0.1
    chunk_frames   = int(SAMPLE_RATE * chunk_duration)
    silence_limit  = int(VAD_SILENCE_SEC / chunk_duration)

    def audio_callback(indata, frames, time_info, status):
        nonlocal recording, silence_frames, audio_buffer, segment_index

        volume = float(np.linalg.norm(indata) / np.sqrt(len(indata)))

        if volume > VAD_THRESHOLD:
            if not recording:
                recording = True
                silence_frames = 0
                print("[REC] Recording participant speech...")
            audio_buffer.extend(indata[:, 0].tolist())
            silence_frames = 0

        elif recording:
            audio_buffer.extend(indata[:, 0].tolist())
            silence_frames += 1

            if silence_frames >= silence_limit:
                recording = False
                duration = len(audio_buffer) / SAMPLE_RATE

                if duration >= MIN_SPEECH_SEC:
                    segment_audio = np.array(audio_buffer, dtype=np.float32)
                    audio_buffer.clear()
                    segment_index += 1
                    idx = segment_index

                    threading.Thread(
                        target=transcribe_segment,
                        args=(model, segment_audio, session_dir,
                              idx, transcript, lock),
                        daemon=True
                    ).start()
                else:
                    audio_buffer.clear()
                    print("[REC] Segment too short — discarded.")

    print("[REC] Recording started. Listening for participant speech...")
    print("      Press Ctrl+C at end of session to save and exit.\n")

    with sd.InputStream(
        samplerate=SAMPLE_RATE,
        channels=CHANNELS,
        dtype='float32',
        blocksize=chunk_frames,
        callback=audio_callback
    ):
        try:
            while not stop_event.is_set():
                time.sleep(0.1)
        except KeyboardInterrupt:
            pass

    # Wait briefly for any in-progress transcriptions to finish
    time.sleep(2)
    save_session(session_dir, transcript, segments)

# ── Entry point ───────────────────────────────────────────────────────────────

def main():
    print("=" * 55)
    print("  Mac Recorder — Culture in HRI Study")
    print("  Imperial College London")
    print("=" * 55 + "\n")

    participant_id, session_dir = setup_session()

    print("[SETUP] Loading Whisper model...")
    model = whisper.load_model(WHISPER_MODEL)
    print(f"[SETUP] Whisper '{WHISPER_MODEL}' loaded.\n")

    stop_event = threading.Event()

    try:
        record_loop(model, session_dir, stop_event)
    except KeyboardInterrupt:
        stop_event.set()
        print("\n\nSession ended.")

if __name__ == "__main__":
    main()
