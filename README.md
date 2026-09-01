# Culture in HRI — Interactive Robot Study System
 
Masters research project, IX, Imperial College London.
 
**Supervisor:** Dr Nicole Salomons  
**Author:** Jude Abussaud  
**Academic year:** 2025–26
 
---
 
## Overview
 
This repository contains the full implementation of an interactive human-robot interaction (HRI) study investigating whether cultural background, specifically Power Distance (PDI) and Uncertainty Avoidance (UAI) as defined by Hofstede's Cultural Dimensions framework, predicts how people respond to different robot communication styles.
 
The study uses a **Reachy Mini** social robot (Pollen Robotics) paired with two custom Unity 6 game-based tasks:
 
- **City Builder** (PDI study): participants place buildings on a 6×6 grid guided by a robot speaking in either a directive or peer-like style
- **Sorting Task** (UAI study): participants sort community resource objects into Urgent / Important / Optional categories guided by a robot speaking in either a predictable or adaptive style
Participants complete a pre-study questionnaire (VSM-2013 cultural values + demographics) and a post-interaction questionnaire (NARS, Godspeed, Trust in Automation, custom Likert items, open responses) administered via Qualtrics.
 
---
 
## Repository Structure
 
```
culture-in-hri/
├── README.md
├── .gitignore
├── unity/
│   └── Scripts/              # All Unity C# scripts
├── robot/
│   └── robot_listener.py     # Runs on Reachy Mini via SSH
├── mac/
│   └── mac_recorder.py       # Runs on Mac — recording + transcription
├── surveys/
│   ├── pre_study_v2.qsf
│   ├── pdi_post_v2.qsf
│   └── uai_post_v2.qsf
└── docs/
    ├── participant_information_sheet.md
    ├── briefing_script.md
    └── debriefing_sheet.md
```
 
---
 
## System Architecture
 
The system has three components running simultaneously during each session:
 
```
Unity 6 (Mac)          Python listener (Mac)         Reachy Mini
──────────────         ─────────────────────         ───────────
GameManager      ①──▶  TCP socket (port 65432)
Task managers          gTTS → MP3 generation   ──▶   Daemon API
uGUI                   VAD + nodding trigger   ──▶   /api/move/set_target
Data logging           Recording + Whisper           /api/media/play_sound
                 ◀──②  Completion signal             Head tracking
                       (word-count heuristic)        Idle wobbling
```
 
**① Dialogue string:** Unity sends the full dialogue text to the Python listener over a local TCP socket immediately before recording the send timestamp for latency measurement.
 
**② Completion signal:** The Python listener sends `b"done"` back to Unity after a word-count-based wait (utterance length ÷ 150 wpm + 0.8s buffer). Note: the Reachy Mini daemon's `/api/media/play_sound` endpoint returns immediately on accepting the playback request rather than on completion, so the completion signal is an estimate rather than a confirmed hardware callback.
 
---
 
## Dependencies
 
### Unity
- Unity 6 (6000.0.x)
- TextMeshPro (included via Package Manager)
- Input System set to **Both** (Project Settings → Player)
### Python (Mac - robot_listener.py and mac_recorder.py)
```
python3.11
gtts
requests
sounddevice
numpy
scipy
openai-whisper
edge-tts          # optional offline TTS alternative to gTTS
```
 
Install with:
```bash
python3.11 -m pip install gtts requests sounddevice numpy scipy openai-whisper
```
 
### Reachy Mini (robot)
- Reachy Mini daemon v1.8.3 or v1.9.0
- `/venvs/apps_venv/` - pre-installed on robot
- gtts installed into apps_venv: `/venvs/apps_venv/bin/pip install gtts`
- Internet connection required on Mac (gTTS calls Google TTS API)
---
 
## How to Run a Session
 
### Before each session
 
1. Confirm the robot's IP address (changes between sessions on some networks):
```bash
   # On robot via SSH:
   hostname -I
```
 
2. Update the host in Unity Inspector:
   - Select `RobotController` GameObject → set `Host` field to robot IP
   - Set `Participant ID` and `Condition` fields
3. Set the condition in the game manager Inspector:
   - **City Builder:** `GameManager` → `Condition` → `PDI_A_Directive` or `PDI_B_PeerLike`
   - **Sorting Task:** `SortingGameManager` → `Condition` → `UAI_A_Predictable` or `UAI_B_Adaptive`
### Session startup (three terminals)
 
**Terminal 1 — Robot listener (SSH into robot):**
```bash
ssh pollen@<robot-ip>
/venvs/apps_venv/bin/python ~/robot_listener.py
```
Wait for: `Waiting for Unity to connect...`
 
**Terminal 2 — Mac recorder:**
```bash
python3.11 mac/mac_recorder.py
```
Enter participant ID when prompted.
 
**Terminal 3 — (optional) re-enable wobbling if robot restarted:**
```bash
curl -X POST http://<robot-ip>:8000/api/media/wobbling/enable
```
 
**Then hit Play in Unity.**
 
### Session end
 
- Stop Play in Unity (latency CSV saves automatically)
- Press `Ctrl+C` in Terminal 2 (transcript and audio segments save to `~/HRI_Study_Data/`)
---
 
## Data Output
 
### City Builder (`DataLogger.cs`)
Saved to `Application.persistentDataPath/SessionData/`:
```
ParticipantID, Condition, BuildingName, BuildingID,
PlacedRow, PlacedCol, RecommendedRow, RecommendedCol,
FollowedRecommendation, DecisionTimeSeconds, PlacementOrder, Timestamp
```
 
### Sorting Task (`SortingDataLogger.cs`)
Saved to `Application.persistentDataPath/SessionData/`:
```
ParticipantID, Condition, ObjectName, ObjectID,
CategoryChosen, DecisionTimeSeconds, SortingOrder, Timestamp
```
**Note:** Decision time is measured from when the robot finishes speaking and buttons become active, not from when the object appears on screen.
 
### Round-trip latency (`RobotController.cs`)
Saved to `~/HRI_Study_Data/latency_[participantID]_[condition]_[timestamp].csv`:
```
UtteranceIndex, SendTimestampMs, ReceiveTimestampMs, RoundTripMs, DialogueText
```
A summary (N, mean, min, max) is also printed to the Unity Console at session end.
 
### Transcription (`mac_recorder.py`)
Saved to `~/HRI_Study_Data/[participantID]_[timestamp]/`:
- `transcript.txt`- timestamped participant speech, transcribed by Whisper
- `segment_NNN.wav` - individual speech segment audio files
---
 
## Robot Behaviour
 
The Reachy Mini exhibits the following behaviours during sessions, all managed via the daemon HTTP API (`http://<robot-ip>:8000`):
 
| Behaviour | Mechanism | Endpoint |
|---|---|---|
| Speech | gTTS MP3 uploaded and played | `/api/media/sounds/upload`, `/api/media/play_sound` |
| Head tracking | Built-in daemon face detection | `/api/media/tracking/enable` |
| Idle movement | Audio-reactive head wobbling | `/api/media/wobbling/enable` |
| Nodding | VAD triggers pitch movement | `/api/move/set_target` |
 
---
 
## Study Design Summary
 
| | PDI Study | UAI Study |
|---|---|---|
| Task | City Builder (6×6 grid, 5 buildings) | Sorting Task (8 objects, 3 categories) |
| Condition A | Directive robot communication | Predictable, rule-governed guidance |
| Condition B | Peer-like, collaborative communication | Adaptive, open-ended guidance |
| Measure | Building placement compliance | Category selection + decision time |
| National groups | UK vs Saudi Arabia | UK vs Saudi Arabia |
| Cultural instrument | VSM-2013 (individual PDI/UAI scores) | VSM-2013 (individual PDI/UAI scores) |
 
---
 
## Known Limitations
 
**Word-count completion heuristic:** The `play_sound` daemon endpoint is non-blocking. Completion is estimated at `(word_count / 150 * 60) + 0.8s`. If actual TTS speech rate diverges from 150 wpm, the "done" signal may lead or lag true playback completion. The 0.8s buffer partially compensates but does not eliminate this error. Round-trip latency figures therefore capture the heuristic wait duration, not true playback duration.
 
**gTTS internet dependency:** Speech generation requires an internet connection on the Mac. For offline operation, replace gTTS with edge-tts (`pip install edge-tts`).
 
**Robot IP instability:** The Reachy Mini's IP address changes between network sessions on some routers. Confirm with `hostname -I` before each session and update `RobotController.cs` accordingly.
 
**Sorting task decision timer:** Decision time is recorded from when the robot finishes speaking (buttons become active), not from when the object appears on screen. Ensure `OnObjectDisplayed()` is called inside `OnRobotFinishedSpeaking()` in `SortingGameManager.cs`.
 
---
 
## Ethics
 
This study was reviewed and approved by the Department of Electrical and Electronic Engineering, Imperial College London. No formal external ethics approval was required. All participants are to provide written informed consent. Data is anonymised at the point of collection and stored on Imperial College London's institutional servers in accordance with GDPR.
 
---
 
## Citation
 
If referencing this system, please cite:
 
> Abussaud, J. (2025). *Designing for Cultural Difference: A Robot-Mediated System for Studying Power Distance and Uncertainty Avoidance in the UK and Saudi Arabia*. Masters project, IX, Imperial College London.
