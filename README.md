# EcoSort VR

## Learning to Sort Waste Through Virtual Reality

EcoSort VR is an educational game developed in Unity for Meta Quest. Players learn to identify and sort waste by picking up virtual objects and placing them in the corresponding bins.

The project was developed for our Human–Computer Interaction course. It combines environmental education, practical interaction, and a gradual learning process.

### Team Members

- Cesar Ernesto Pérez Gómez
- Iker Jesús Ortiz Rojero
- Rodrigo Zatarain Aguirre

## The Problem

Waste management is an environmental challenge in Mexico. Understanding how to separate waste is part of addressing this problem, but knowing the names of different categories does not always mean knowing where to place an everyday object.

We also saw an opportunity to use technology for a clear educational purpose. Children may be familiar with digital devices, but they still need guidance when learning a new topic or using an unfamiliar interface.

Our project brings these ideas together: giving children a practical way to explore waste classification through an interactive activity.

## Target Users

Our main users are school-age children who are beginning to learn about waste separation and environmental responsibility. The experience also considers players who have little or no previous experience with virtual reality.

Teachers and families can use the game as a supervised activity to support discussions about recycling and waste management.

The interface provides Spanish and English language options, introductory instructions, hints, and feedback to help players understand what to do.

## Our Solution

EcoSort VR places the player in a virtual environment where they can practice sorting waste. Instead of only reading about different categories, players interact with objects and receive feedback on their decisions.

The experience begins with a tutorial and continues through nine levels. Players encounter different combinations of objects and bins as they progress.

**EcoSort VR is intended to complement traditional education.** It provides an additional practical activity that can support lessons taught at school or at home.

## Gameplay

### Tutorial

The tutorial introduces the basic interaction: picking up a cube and releasing it inside a designated drop zone.

### Nine Sorting Levels

Each level follows three main stages:

1. **Introduction:** The player reads the objective and instructions.
2. **Practice:** The player places the waste objects into their matching bins and can request a hint.
3. **Results:** The player reviews their stars, attempts, and hints used before continuing or repeating the level.

After completing Level 9, the player can select **Repeat** to play the level again or **Finish** to return to the main menu.

### Waste Categories

The game uses the following color-to-category mapping:

| Bin color | Category |
|---|---|
| Green | Organic waste |
| Grey | General waste |
| Red | Sharps and needles |
| Blue | Inorganic recyclable waste |
| Yellow | Glass |
| Orange | Special waste, such as batteries |

These are the categories used within the game. Real-world sorting instructions should follow the local collection system.

## Main Features

- An introductory tutorial and nine playable levels.
- Interactive objects that players can pick up and sort.
- Written feedback for correct and incorrect placements.
- A hint button that identifies the matching bin.
- Progress and attempt indicators during gameplay.
- Result screens with stars, attempts, and hints used.
- Spanish and English interface options.
- A virtual outdoor environment.

## How to Run the Application

### Requirements

To build and run the project on a headset, you need:

- Unity Hub.
- **Unity 6000.5.7f1**.
- Android Build Support, including Android SDK & NDK Tools and OpenJDK.
- A Meta Quest headset. The project has been tested on **Meta Quest 3**.
- Quest controllers.
- A USB cable that supports data transfer.
- Developer Mode enabled on the headset.
- Internet access for the initial Unity installation and package download.

Install the Android modules through Unity Hub when installing the editor, or add them afterward through **Installs → Manage → Add modules**.

### 1. Download the Project

Clone the repository using Git:

```bash
git clone https://github.com/CesarErnestoPerezGomez/HCIFinalProject.git
```

Alternatively, download the repository through **Code → Download ZIP** on GitHub and extract it.

### 2. Open the Project in Unity

1. Open **Unity Hub**.
2. Select **Add project from disk**.
3. Select the repository's root folder, which contains `Assets`, `Packages`, and `ProjectSettings`.
4. Open it using the specified Unity version.
5. Wait for Unity to import the assets and restore the required packages.

### 3. Open the Main Menu

In Unity's Project window, open:

```text
Assets/Scenes/ScenesOfTheProject/MainMenu.unity
```

The main menu is the starting point for the complete experience.

### 4. Check the Build Configuration

1. Open **File → Build Profiles**.
2. Select **Android** and switch to that platform if it is not already active.
3. Open the scene list.
4. Confirm that the following scenes are enabled, with `MainMenu` first:

```text
MainMenu
TutorialLevel
Level1
Level2
Level3
Level4
Level5
Level6
Level7
Level8
Level9
```

These scenes are located in `Assets/Scenes/ScenesOfTheProject`.

### 5. Connect the Meta Quest

1. Turn on the headset and connect it to the computer using the USB cable.
2. Put on the headset and accept the **USB debugging** authorization.
3. Return to Unity.
4. In Build Profiles, click **Refresh** beside **Run Device**.
5. Select the connected headset.

If the headset does not appear, check Developer Mode, USB debugging authorization, and whether the cable supports data transfer. Windows may also require the device's USB driver.

For additional connection instructions, see [Unity's Android device documentation](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/android/developing/testing-and-debugging/debugging-on-an-android-device).

### 6. Build and Play

1. Keep **Build App Bundle (Google Play)** disabled to create an APK.
2. Select **Build and Run**.
3. Choose a location and filename for the APK.
4. Wait for Unity to build the application, install it on the headset, and launch it.
5. Select the desired language in the main menu and start the tutorial.

For later sessions, open the installed application from the headset's app library. Sideloaded applications may appear under **Unknown Sources**.

### Optional: Run from the Unity Editor

You can also test the project through Meta Horizon Link on a compatible Windows computer. Configure the headset connection and the desktop OpenXR settings, open `MainMenu`, and enter Play Mode.

Follow [Unity's Meta Horizon Link setup guide](https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.5/manual/get-started/link.html) for this workflow.

## Controls

| Action | Interaction |
|---|---|
| Select a UI button | Point at the button with the controller and press the trigger. |
| Pick up an object | Move the controller to the object and hold the grip button. |
| Release an object | Release the grip button over the intended bin or drop zone. |
| Request help | Select the **Hint** button. |
| Change language | Select the language button in the main menu. |

## Design and User Testing

We developed the interface flow in Figma before implementing it in Unity. The design includes level introductions, practice screens, feedback, and results.

We conducted usability testing with three participants. The sessions helped us identify interaction difficulties and areas where the interface needed improvement, including the visibility and positioning of gameplay information and the hint button.

The research, observations, and documented improvements are available in the project documentation.

## Project Documentation

| Document | Contents |
|---|---|
| [User Research](Docs/User%20Research%20md.pdf) | Research methods, findings, user needs, behaviours, constraints, and customer journey. |
| [Design Justification](Docs/Design%20Justification%20md.pdf) | Main design decisions and their relationship to HCI concepts. |
| [User Testing](Docs/UserTesting%20md.pdf) | Testing tasks, participant observations, results, and improvements. |
| [AI Usage](Docs/AI%20Usage%20md.pdf) | Documentation of AI assistance during the project. |

### Figma Prototype

[Open the EcoSort VR prototype](https://www.figma.com/design/uhU0gue7RGnwHJi4CQLm14/HCI-FInal-Project?t=tmT2LNHMGYIiIBWG-0)

### Commercial

[View or download the EcoSort VR commercial](EcoSort-AD.mp4)

If GitHub does not display a preview, use the download button on the video's file page.
