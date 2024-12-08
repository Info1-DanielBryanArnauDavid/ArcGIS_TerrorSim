# ArcGIS TerrorSim Project

## Overview

**ArcGIS TerrorSim** is a simulation tool designed to load, manage, and visualize flight plans and waypoints. It integrates data from `.txt` files and uses ArcGIS for geographic visualization. The project includes classes for flight plan management, waypoints, and support for multiple flight plans. The aim is to create a simulation environment where users can explore flight paths, flight levels, and speeds based on a set of waypoints.

![ArcGIS TerroSim Logo](https://i.ibb.co/hKq6fzq/logo.jpg)

---

## Features

- **Waypoint Management**: Load waypoints from `.txt` files that contain waypoint names and geographic coordinates (latitude and longitude).
- **Flight Plan Management**: Load flight plans with associated waypoints, flight levels (FLXX), and speeds (KTXX) from `.txt` files.
- **Data Parsing**: Automatically parse and load flight plans based on time, company, waypoint, and speed/flight level data.
- **Simulation Control**: Simulate flights by defining the path using waypoints and displaying flight data in ArcGIS's scene view.
- **Extensible**: The system is designed for future extensions to support additional simulation features.

---

## References

-https://aip.enaire.es/AIP/contenido_AIP/ENR/LE_ENR_4_4_es.html

-https://www.eurocontrol.int/publication/free-route-airspace-fra-points-list-ecac-area

-https://opennav.com/

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

5. For the sample showcase, and what we have tested decently, use these waypoints and flightplans (insertar link relativo)

6. If you feel more experimental try these (insertar link relativo), which could have some untested problems.

---

## Example Usage

### 1. Waypoint Data File Example (waypoints.txt)
 ```bash
VERSO,41.153,3.757
VETAN,41.962,-5.716
VETAR,42.178,-0.493
VIBAS,37.392,-3.631
VIBIM,41.071,2.207
VIBOK,41.547,1.502
VICAR,37.251,-5.782
VIGIA,36.938,-6.573
VILAR,41.342,0.566
VILGA,40.764,1.895
VILLA,40.233,-2.41
VILNA,38.54,-0.817
VIRTU,40.562,-2.499
VIZON,27.682,-16.789
VULPE,37.761,-4.798
WALLY,39.754,-1.094
XALUD,39.0,-4.962
XAMUR,41.403,2.872
XANOS,27.936,-16.865
XARON,38.405,2.854
XAVIR,36.004,-5.243
```

### 2. Flight Plan Data File Example (flightplans.txt)
 ```bash
11:01:56,KLM Royal Dutch Airlines,KL8241,A320
PNA,428m,120
GOSVI,FL50,199
RONKO,FL80,279
SURCO,FL170,359
MARIO,FL310,439
POSSY,FL310,439
GRAUS,FL310,439
MECKI,FL310,439
ROVAP,FL310,439
KARES,FL310,439
BANBU,FL310,439
NEPAL,FL310,439
CORDA,FL230,359
LISAS,FL50,279
TUENT,FL50,199
PMI,58m,120
11:22:21,SAS (Scandinavian Airlines),SK3481,A320
ALC,21m,120
VILNA,FL50,201
NARGO,FL80,283
OLPOS,FL170,365
POBOS,FL310,447
TOSTO,FL310,447
ETURA,FL310,447
BUDIT,FL310,447
KOSEL,FL310,447
BAZAS,FL310,447
USERA,FL310,447
LANCE,FL310,447
ALCOL,FL310,447
ASBUM,FL230,365
GENIL,FL50,283
ROTEX,FL50,201
SVQ,35m,120
```
### 3. Flight Plan Generation

We use an adapted Djikstra algorithm for the generation of procedural flightplans, by taking the Waypoints file and defining a radius of connection, we can simulate a decent graph for the algorithm to solve. By also adding a threshold for margins, we observe decently realistic flightplans as they vary in a couple of waypoints. By generating a random pair of airports, we can find the path and create realistic FLs and speeds per each stage of the flight. 

### 4. Waypoint extraction

We used diverse references for the extraction of the waypoints and airports with their corresponding altitudes and data. We have defined three differetn spaces, Spain, Europe and Global, with a comprehensive list each with the corresponding ENR or enroute waypoints, we have not taken into account the STARs or SIDs for each procedure, something to keep in mind for the realism of the flight plans.
