# Unity Autopilot

   Unity Autopilot is an AI-powered tool integrated into the Unity Editor, allowing developers and creators to control the editor using natural language commands—speeding up workflows by automating tasks described in plain English.

<img src="Info/autopilot-window.png" alt="autopilot window" width="300" style="display: block; margin: auto;"/>


## ✨ Features

- 🧠 **Natural Language Command Execution** – Translate plain English into Unity editor actions.
- 🛠️ **Editor Automation** – Create GameObjects, modify components, manage scenes, and more through text.
- 🔌 **Customizable & Extensible** – Easily define your own command handlers and integrate with different LLM providers.
- 🤖 **Any LLM Support** – Possible to use any LLM api through a generalized API backend.

## 📊 Project Tracking

| Task                                      | Status                |
| ----------------------------------------- | --------------------- |
| Generalize LLM Communication API          | ✅ Done                |
| Manage Script & Shader Compilation Timing | ✅ Requires Testing    |
| Develop Comprehensive Tool Testing Suite  | ⚠️ Partially Complete  |
| Implement UI Builder-Based GUI            | ⏳ To Do               |
| Implement Support for Additional LLM APIs | ⏳ To Do               |
| Integrate Markdown Text Viewer            | ⏳ To Do               |
| Implement Multi-Agent Flow Architecture   | ⏳ To Do               |
| └─ Task Manager Agent                     | ⏳ To Do               |
| └─ Log Reader Agent                       | ⏳ To Do               |


## 🚀 Getting Started

### Requirements

- Unity **2021.3 LTS** or later
- Internet connection (for online LLMs)
- API key for your preferred language model provider (e.g., OpenAI, Azure, local)



## 📦 Installation

### Step 1 – Add OpenUPM as a scoped registry

1. Open `Edit > Project Settings > Package Manager > Scoped Registries`
2. Click **Add** and fill in the following:
   - **Name**: `OpenUPM`
   - **URL**: `https://package.openupm.com`
   - **Scope(s)**:
     - `com.cysharp.unitask`
     - `com.openai`
     - `com.utilities`

### Step 2 – Install required dependencies

Open Unity's **Package Manager**, switch to `My Registries`, and install:

#### From scoped registry:
- `com.utilities.rest` – version `3.3.3`
- `com.utilities.websockets` – version `1.0.3`
- `com.utilities.encoder.wav` – version `2.2.2`
- `com.cysharp.unitask` - version `2.5.10`

#### From Git URL:
- **OpenAI for Unity**  
[https://github.com/bhadrik/com.openai.unity.git?path=/OpenAI/Packages/com.openai.unity#main](https://github.com/bhadrik/com.openai.unity.git?path=/OpenAI/Packages/com.openai.unity#main)


### Step 3 – Install Unity Autopilot

- **Install the Unity Autopilot package via Git URL**  
   [https://github.com/bhadrik/unity-autopilot.git?path=/Package#main](https://github.com/bhadrik/unity-autopilot.git?path=/Package#main)

### Step 4 - Run Autopilot

- **Window location:** `Window/Autopilot/Chat`
---

### Scoped registry
![Scoped Registry Configuration](Info/scpoed-registory.png)
### Package manager
![Scoped Registry Configuration](Info/package-manager-installed.png)


<br>

# Dependency

## Newtonsoft.Json.Schema

This project includes a pre-configured dependency on the Newtonsoft.Json.Schema library, version 3.0.16.

### About Newtonsoft.Json.Schema

Newtonsoft.Json.Schema is a .NET library used for validating JSON data against JSON Schema specifications. It provides a powerful and flexible way to ensure JSON data conforms to expected structures.

### Useful Links

- Official website: [https://www.newtonsoft.com/jsonschema](https://www.newtonsoft.com/jsonschema)
- GitHub Releases: [https://github.com/JamesNK/Newtonsoft.Json.Schema/releases](https://github.com/JamesNK/Newtonsoft.Json.Schema/releases)


## com.openai.unity

This is an unofficial OpenAI package to interact with the OpenAI API.

### About com.openai.unity

This package is a modified version of the original repository available at [RageAgainstThePixel/com.openai.unity](https://github.com/RageAgainstThePixel/com.openai.unity).

### Useful Links

- Original Repository: [https://github.com/RageAgainstThePixel/com.openai.unity](https://github.com/RageAgainstThePixel/com.openai.unity)
- Forked Repository: [https://github.com/bhadrik/com.openai.unity](https://github.com/bhadrik/com.openai.unity)

<br>

# Source File Origins and Adaptations

This project utilizes several source files originally taken from the [unity-mcp](https://github.com/justinpbarnett/unity-mcp) repository by Justin P. Barnett as a foundational base. 

All these files have since been modified and adapted to meet the specific requirements of this current project.
