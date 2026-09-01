#!/usr/bin/env python3
"""
robot_listener.py - runs ON THE ROBOT via SSH
Receives text from Unity, speaks via robot speaker, sends done signal back.
"""

import socket
import threading
import time
import os
import tempfile
import requests
import numpy as np

HOST          = '0.0.0.0'
PORT          = 65432
ROBOT_API     = 'http://localhost:8000'
VAD_THRESHOLD = 0.05
SAMPLE_RATE   = 16000

robot_speaking = False

def speak(text):
    global robot_speaking
    robot_speaking = True
    try:
        from gtts import gTTS
        tmp_mp3 = tempfile.NamedTemporaryFile(suffix='.mp3', delete=False).name
        tts = gTTS(text=text, lang='en', slow=False)
        tts.save(tmp_mp3)
        filename = f"speech_{int(time.time())}.mp3"
        with open(tmp_mp3, 'rb') as f:
            requests.post(f"{ROBOT_API}/api/media/sounds/upload",
                         files={'file': (filename, f, 'audio/mpeg')}, timeout=10)
        requests.post(f"{ROBOT_API}/api/media/play_sound",
                     json={"file": filename}, timeout=10)
        word_count = len(text.split())
        time.sleep((word_count / 150 * 60) + 0.8)
        os.unlink(tmp_mp3)
    except Exception as e:
        print(f"[SPEAK] Error: {e}")
    finally:
        robot_speaking = False

def nod():
    try:
        requests.post(f"{ROBOT_API}/api/move/set_target",
                     json={"head": {"pitch": 15}, "duration": 0.3}, timeout=5)
        time.sleep(0.4)
        requests.post(f"{ROBOT_API}/api/move/set_target",
                     json={"head": {"pitch": 0}, "duration": 0.3}, timeout=5)
        print("[NOD] Nodded.")
    except Exception as e:
        print(f"[NOD] Error: {e}")

def vad_loop(stop_event):
    try:
        import sounddevice as sd
    except ImportError:
        print("[VAD] sounddevice not available.")
        return
    was_speaking = False
    chunk_frames = int(SAMPLE_RATE * 0.1)
    def callback(indata, frames, time_info, status):
        nonlocal was_speaking
        if robot_speaking:
            was_speaking = False
            return
        volume = float(np.linalg.norm(indata) / np.sqrt(len(indata)))
        if volume > VAD_THRESHOLD and not was_speaking:
            was_speaking = True
            threading.Thread(target=nod, daemon=True).start()
        elif volume <= VAD_THRESHOLD:
            was_speaking = False
    try:
        with sd.InputStream(samplerate=SAMPLE_RATE, channels=1,
                           dtype='float32', blocksize=chunk_frames,
                           callback=callback):
            while not stop_event.is_set():
                time.sleep(0.1)
    except Exception as e:
        print(f"[VAD] Error: {e}")

def face_tracking_loop(stop_event):
    print("[FACE] Face following started.")
    while not stop_event.is_set():
        try:
            r = requests.get(f"{ROBOT_API}/api/media/tracking/face", timeout=3)
            data = r.json()
            if data.get("face_target", {}).get("detected"):
                x = data["face_target"]["x"]
                # x is roughly -1 to 1, map to yaw degrees
                yaw = -x * 30
                requests.get(f"{ROBOT_API}/api/media/tracking/face", timeout=5)
        except Exception as e:
            print(f"[FACE] Error: {e}")
        time.sleep(0.5)
    print("[FACE] Face following stopped.")

def main():
    print("=" * 50)
    print("  Robot Listener - Culture in HRI Study")
    print("  Imperial College London")
    print("=" * 50)

    try:
        from gtts import gTTS
    except ImportError:
        print("Installing gTTS...")
        os.system("/venvs/apps_venv/bin/pip install gtts")

    stop_event = threading.Event()
    threading.Thread(target=vad_loop, args=(stop_event,), daemon=True).start()
    threading.Thread(target=face_tracking_loop, args=(stop_event,), daemon=True).start()

    try:
        requests.post(f"{ROBOT_API}/api/media/tracking/enable", timeout=5)
        requests.post(f"{ROBOT_API}/api/media/tracking/face", timeout=5)
        print("[FACE] Face tracking enabled.")
    except Exception as e:
        print(f"[FACE] Error enabling tracking: {e}")

    try:
        requests.post(f"{ROBOT_API}/api/media/wobbling/enable", timeout=5)
        print("[MOVEMENT] Wobbling enabled.")
    except Exception as e:
        print(f"[MOVEMENT] Error enabling wobbling: {e}")
    
    print(f"\nListening for Unity on port {PORT}")
    print("Start Unity and hit Play to connect.\n")

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server.bind((HOST, PORT))
        server.listen()
        try:
            while True:
                print("Waiting for Unity to connect...")
                conn, addr = server.accept()
                with conn:
                    print(f"Unity connected from {addr[0]}:{addr[1]}")
                    print("-" * 40)
                    while True:
                        try:
                            data = conn.recv(4096)
                            if not data:
                                print("Unity disconnected.")
                                break
                            text = data.decode('utf-8').strip()
                            if not text:
                                continue
                            print(f"\n[SPEAKING] {text}")
                            speak(text)
                            print("[DONE]")
                            conn.sendall(b"done")
                        except ConnectionResetError:
                            print("Connection reset.")
                            break
                        except Exception as e:
                            print(f"Error: {e}")
                            break
                    print("-" * 40)
        except KeyboardInterrupt:
            print("\nStopping...")
    stop_event.set()
    print("Done.")

if __name__ == "__main__":
    main()
