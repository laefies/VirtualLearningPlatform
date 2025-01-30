# Collaborative AR/VR Learning Platform

## What is it?
Multi-device collaborative system that allows users to interact in a shared learning environment using AR and VR.

## Structure

### 📁 Scripts/
All C# organized by functionality.

#### Core/
- **NetworkManager**: Connection management between devices, state synchronization.
- **WorldManager**:   Shared environmente management and global state.
- **DeviceManager**:  Abstraction for the various devices.

#### AR/
- **MarkerDetection/**
  - Image processing and alignment system;
  - Specific implementation for various objects and libraries;
- **Devices/**
  - Specific implementations for various AR devices;
  - Abstractions and shared interfaces;

#### VR/
- **Devices/**
  - Specific implementations for various AR devices;
  - Abstractions and shared interfaces;

#### Debug/
- Logging and debugging system;
- In-game console for easy debugging;

### 📁 Prefabs/
Project prefabs.

- **UI/**: Interface elements, including debugging console;
- **Markers/**: Visualizers and other utils regarding marks and detectable objects;

### 📁 Resources/
Resources and configuration files.

## Environment Configuration

### Requirements
- Unity 2022.3 LTS or newer;
- Packages:
  - AR Foundation
  - XR Interaction Toolkit

### Installation
1. Clone the repository;
2. Open the project on Unity Hub;
3. Install via Package Manager all necessary packages;
4. Configure XR settings as the documentation describes;
