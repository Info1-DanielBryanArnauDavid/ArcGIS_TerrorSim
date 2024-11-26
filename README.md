# ArcGIS TerroSim Project

## Overview

**ArcGIS TerroSim** is a simulation tool designed to load, manage, and visualize flight plans and waypoints. It integrates data from `.txt` or `.csv` files and uses ArcGIS for geographic visualization. The project includes classes for flight plan management, waypoints, and support for multiple flight plans. The aim is to create a simulation environment where users can explore flight paths, flight levels, and speeds based on a set of waypoints.

![ArcGIS TerroSim Logo](https://es.m.wikipedia.org/wiki/Archivo:Airbus_Beluga_Airbus_A300B4-608ST_F-GSTA_(28858044414).jpg)

---

## Features

- **Waypoint Management**: Load waypoints from `.txt` or `.csv` files that contain waypoint names and geographic coordinates (latitude and longitude).
- **Flight Plan Management**: Load flight plans with associated waypoints, flight levels (FLXX), and speeds (KTXX) from `.csv` files.
- **Data Parsing**: Automatically parse and load flight plans based on time, company, waypoint, and speed/flight level data.
- **Simulation Control**: Simulate flights by defining the path using waypoints and displaying flight data in ArcGIS's scene view.
- **Customizable UI**: The welcome window provides instructions and a clean, semi-transparent UI with a close button.
- **Extensible**: The system is designed for future extensions to support additional simulation features.

---

## Installation

To run this project locally, you need:

- **Visual Studio** (or another C# development environment)
- **.NET Core or .NET Framework** (depending on your setup)
- **ArcGIS Runtime SDK for .NET** (for geographic visualization)

### Steps to set up:

1. Clone the repository:
    ```bash
    git clone https://github.com/yourusername/ArcGIS_TerroSim.git
    ```

2. Open the solution file (`ArcGIS_TerroSim.sln`) in **Visual Studio**.

3. Ensure the **ArcGIS Runtime SDK for .NET** is installed:
    - Download and install ArcGIS Runtime SDK from [ArcGIS for Developers](https://developers.arcgis.com/net/).

4. Build and run the project in Visual Studio.

---

## Example Usage

### 1. Waypoint Data File Example (waypoints.txt)
### 2. Flight Plan Data File Example (flightplans.csv)
