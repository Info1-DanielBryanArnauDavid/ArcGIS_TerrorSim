import random
from bs4 import BeautifulSoup

def convert_to_decimal(degree_string):
    """Convert HHMMSS.S direction format to decimal degrees."""
    if len(degree_string) < 9:
        return None
    if degree_string[-1] in ['N', 'S']:
        degrees = int(degree_string[:2])
        minutes = int(degree_string[2:4])
        seconds = float(degree_string[4:-1])
    else:
        degrees = int(degree_string[:3])
        minutes = int(degree_string[3:5])
        seconds = float(degree_string[5:-1])
    direction = degree_string[-1]
    decimal_degrees = degrees + (minutes / 60) + (seconds / 3600)
    if direction in ['S', 'W']:
        decimal_degrees *= -1
    return decimal_degrees

def extract_waypoints_from_html(file_path):
    """Extract waypoints from the HTML file."""
    with open(file_path, 'r', encoding='utf-8') as file:
        html_content = file.read()

    soup = BeautifulSoup(html_content, 'html.parser')
    rows = soup.find_all('tr')
    waypoints = []

    for row in rows:
        ident_cell = row.find('td', class_='celIDENT')
        coords_cell = row.find('td', class_='celCOORDS')

        if ident_cell and coords_cell:
            waypoint_name = ident_cell.get_text(strip=True)
            coords_text = coords_cell.get_text(strip=True)
            latitude_hhmmss = coords_text[:9]
            longitude_hhmmss = coords_text[9:]
            latitude_decimal = convert_to_decimal(latitude_hhmmss)
            longitude_decimal = convert_to_decimal(longitude_hhmmss)

            if latitude_decimal is not None and longitude_decimal is not None:
                latitude_decimal_rounded = round(latitude_decimal, 3)
                longitude_decimal_rounded = round(longitude_decimal, 3)
                if len(waypoint_name) == 5:  # Ensure 5-letter waypoints
                    waypoints.append(f"{waypoint_name},{latitude_decimal_rounded},{longitude_decimal_rounded}")
    return waypoints

def generate_mesh():
    """Generate a broad mesh of waypoints between Spain and the Canary Islands."""
    mesh_waypoints = []
    waypoint_names = set()  # Ensure unique 5-letter names

    def random_waypoint_name():
        while True:
            name = ''.join(random.choices("ABCDEFGHIJKLMNOPQRSTUVWXYZ", k=5))
            if name not in waypoint_names:
                waypoint_names.add(name)
                return name

    # Define the latitude and longitude ranges
    latitudes = [round(x, 3) for x in list(range(28, 44))]  # Canary Islands (~28°N) to Galicia (~43°N)
    longitudes = [round(x, 3) for x in list(range(-13, -5))]  # Canary Islands (~-13°W) to Gibraltar (~-6°W)

    # Generate ~200 points scattered across this area
    for _ in range(200):
        lat = round(random.uniform(28.0, 43.0), 3)
        lon = round(random.uniform(-13.0, -6.0), 3)
        name = random_waypoint_name()
        mesh_waypoints.append(f"{name},{lat},{lon}")

    return mesh_waypoints

def add_airports():
    """Add the specified airports with their coordinates."""
    airports = [
        "MAD,40.493,-3.567",
        "BCN,41.297,2.083",
        "AGP,36.675,-4.499",
        "ALC,38.282,-0.558",
        "PMI,39.551,2.738",
        "IBZ,38.872,1.373",
        "VLC,39.491,-0.481",
        "SVQ,37.418,-5.893",
        "BIO,43.301,-2.910",
        "LPA,27.931,-15.386",
        "TFN,28.482,-16.341",
        "TFS,28.044,-16.572",
        "ACE,28.945,-13.605",
        "FUE,28.452,-13.863",
        "SCQ,42.896,-8.415",
        "LCG,43.302,-8.377",
        "OVD,43.563,-6.034",
        "SDR,43.427,-3.820",
        "ZAZ,41.666,-1.041",
        "GRX,37.188,-3.778",
        "LEI,36.849,-2.370",
        "REU,41.147,1.167",
        "GRO,41.901,2.760",
        "PNA,42.770,-1.646",
        "VGO,42.231,-8.627",
        "MAH,39.862,4.218",
        "XRY,36.744,-6.060",
        "MLN,35.879,-5.316",
        "SPC,28.626,-17.755"
    ]
    return airports

# Main script
file_path = "C:\\Users\\bolty\\Desktop\\ENAIRE AIP ENR 4.4.html"  # Path to HTML file
output_file_path = "C:\\Users\\bolty\\Desktop\\waypoints.txt"

waypoints_list = extract_waypoints_from_html(file_path)
mesh_waypoints = generate_mesh()
airports = add_airports()

# Combine all waypoints
all_waypoints = waypoints_list + mesh_waypoints + airports

# Save to a text file
with open(output_file_path, 'w', encoding='utf-8') as output_file:
    for waypoint in all_waypoints:
        output_file.write(waypoint + "\n")

print(f"Waypoints saved to {output_file_path}")
