# Collaborative AR/VR Learning Platform

## What is it?
Multi-device collaborative system that allows users to interact in a shared learning environment using AR and VR.

At present, the current devices are supported:
- **MagicLeap2**;

## Structure

### 📁 Scripts/
All C# organized by functionality.

#### AR/
- **Devices/**
  - Specific implementations for various AR devices;
  - Abstractions and shared interfaces;

#### Test/
- Provisory, debugging or general testing *scripts*;

#### Vision/
- Classes and structures related to marker detection and object alignment;

### 📁 Prefabs/
Project prefabs, separated by tests.

## Environment Configuration

### Requirements
- Unity 2022.3 LTS or newer;
- Packages:
  - AR Foundation
  - XR Interaction Toolkit

### Installation
1. Clone the repository;
2. Open the project on Unity Hub;
3. Install via Package Manager all packages stated above;
4. Configure XR settings;