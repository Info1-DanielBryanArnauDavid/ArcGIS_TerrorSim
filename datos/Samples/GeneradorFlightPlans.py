import random
import time
import math
import heapq


def read_waypoints(waypoint_file):
    waypoints = {}
    with open(waypoint_file, 'r') as file:
        for line in file:
            parts = line.strip().split(',')
            if len(parts) == 3:
                name = parts[0]
                lat = float(parts[1])
                lon = float(parts[2])
                waypoints[name] = (lat, lon)
    return waypoints


def read_airports(airport_file):
    airports = {}
    with open(airport_file, 'r') as file:
        for line in file:
            parts = line.strip().split(',')
            if len(parts) == 2:
                id = parts[0]
                alt = int(parts[1])
                airports[id] = alt
    return airports


def haversine(coord1, coord2):
    R = 6371  # Radius of the Earth in kilometers
    lat1, lon1 = coord1
    lat2, lon2 = coord2
    lat1_rad, lon1_rad = math.radians(lat1), math.radians(lon1)
    lat2_rad, lon2_rad = math.radians(lat2), math.radians(lon2)
    dlon = lon2_rad - lon1_rad
    dlat = lat2_rad - lat1_rad
    a = math.sin(dlat / 2) ** 2 + math.cos(lat1_rad) * math.cos(lat2_rad) * math.sin(dlon / 2) ** 2
    c = 2 * math.asin(math.sqrt(a))
    return R * c


def build_waypoint_graph(waypoints, max_jump_distance=70):
    """Build a graph where waypoints within max_jump_distance km are connected."""
    graph = {wp: [] for wp in waypoints}
    i=0
    for wp1 in waypoints:
        for wp2 in waypoints:
            if wp1 != wp2:
                distance = haversine(waypoints[wp1], waypoints[wp2])
                if distance <= max_jump_distance:
                    graph[wp1].append((wp2, distance))
        print(i)
        i+=1
    print("Graph Completed")
    return graph


def dijkstra(graph, start, end, randomize=False, diversion_factor=0.2):
    queue = [(0, start, [])]  # (cost, current_node, path)
    visited = set()

    while queue:
        cost, node, path = heapq.heappop(queue)
        if node in visited:
            continue

        visited.add(node)
        path = path + [node]

        if node == end:
            return path, cost

        for neighbor, weight in graph[node]:
            if neighbor not in visited:
                adjusted_weight = weight * (1 + random.uniform(-diversion_factor, diversion_factor)) if randomize else weight
                heapq.heappush(queue, (cost + adjusted_weight, neighbor, path))

    return None, float('inf')  # No path found


# Airline data
airlines = {
    "Air France": "AF",
    "Alitalia Cargo": "AZ",
    "Austrian Airlines": "OS",
    "British Airways": "BA",
    "British Midland Airways": "BD",
    "EVA Airways": "BR",  # Although not a European airline, it's included under the European network
    "KLM Royal Dutch Airlines": "KL",
    "Lufthansa Cargo": "LH",
    "Swiss World Cargo": "LX",
    "Malev Air Cargo": "MA",
    "Malaysia Airlines": "MH",  # Another international carrier with European connections
    "Qantas Airways": "QF",  # Often included in international European routes
    "Scandinavian Airlines": "SK",
    "Finnair": "AY",
    "Iberia Airlines": "IB",
    "Aeroflot": "SU",  # Based in Russia but often included in the European airspace
    "Turkish Airlines": "TK",
    "Brussels Airlines": "SN",
    "Norwegian Air Shuttle": "DY",
    "Vueling": "VY",
    "Wizz Air": "W6",
    "Ryanair": "FR",
    "EasyJet": "U2",
    "Air Portugal (TAP)": "TP",
    "Luxair": "LG",
    "Air Europa": "UX",
    "Czech Airlines": "OK",
    "SAS (Scandinavian Airlines)": "SK",
    "Flybe": "BE",
    "Jet2.com": "LS",
    "Air Malta": "KM",
    "Swiss International Air Lines": "LX",
    "Ukraine International Airlines": "PS",
    "LOT Polish Airlines": "LO",
    "Aer Lingus": "EI",
    "Norwegian Air International": "D8",
    "S7 Airlines": "S7",  # Russian airline, but often flying within Europe
    "TUI Airways": "BY",
    "Hellenic Imperial Airways": "HI",
    "Montenegro Airlines": "YM",
    "Corendon Airlines": "XC"
}


def generate_random_flight_plans(waypoints, airports, num_plans=1):
    flight_plans = []
    graph = build_waypoint_graph(waypoints)  # Build the waypoint graph

    airport_ids = list(airports.keys())  # Get airport IDs dynamically from the `airports` dictionary

    for _ in range(num_plans):
        airline_name = random.choice(list(airlines.keys()))
        airline_prefix = airlines[airline_name]
        flight_number = str(random.randint(1000, 9999))
        callsign = f"{airline_prefix}{flight_number}"

        start_airport, end_airport = random.sample(airport_ids, 2)
        # Get the altitudes for the start and end airports from the airports dictionary
        start_altitude = airports[start_airport]
        end_altitude = airports[end_airport]
        print(start_airport)
        print(end_airport)
        # Decide whether to use the optimal path or a randomized path
        use_randomized_path = random.choice([True, False])  # 50% chance for randomness
        diversion_factor = random.uniform(0.1, 0.3)  # Randomize how suboptimal the path is

        # Find the path using Dijkstra's algorithm
        path, _ = dijkstra(graph, start_airport, end_airport, randomize=use_randomized_path, diversion_factor=diversion_factor)
        if not path or len(path) < 2:
            continue  # Skip if no valid path is found

        # Ensure airports are only first and last waypoints
        path = [start_airport] + [wp for wp in path[1:-1] if wp not in airport_ids] + [end_airport]

        # Generate the flight plan
        timestamp = time.strftime("%H:%M:%S", time.localtime(random.randint(28800, 39600)))
        plan = f"{timestamp},{airline_name},{callsign},A320\n"
        plan += f"{start_airport},{round(start_altitude, 2)}m,120\n"

        # Generate altitude and speed profiles for the waypoints
        max_fl = random.randint(30, 40) * 10  # Random cruise altitude (FL300 - FL400)

        min_speed, max_speed = 120, 490  # Max cruise speed capped at 490 knots

        # Calculate the cruise speed based on the length of the flight (number of waypoints)
        num_waypoints = len(path)
        cruise_speed = min(max_speed, 320 + (num_waypoints - 2) * (490 - 320) / (20))  # Gradually approach 490kt with more waypoints

        climb_waypoints = min(5, len(path) // 4)
        descent_waypoints = min(5, len(path) // 4)

        for i, waypoint in enumerate(path[1:-1], 1):
            if i <= climb_waypoints:  # Climb phase
                # Use quadratic function for smoother climb
                fl = round(max_fl * (i / climb_waypoints) ** 2, -1)
                fl = max(fl, 50)
                speed = min_speed + ((cruise_speed - min_speed) / climb_waypoints) * i
            elif i >= len(path) - descent_waypoints - 1:  # Descent phase
                descent_progress = i - (len(path) - descent_waypoints - 1)
                # Use quadratic function for smoother descent
                fl = round(max(end_altitude / 100, max_fl - (max_fl / descent_waypoints) * (descent_progress ** 2)), -1)
                fl = max(fl, 50)
                speed = max(min_speed,
                            cruise_speed - ((cruise_speed - min_speed) / descent_waypoints) * descent_progress)
            else:  # Cruise phase
                # Randomize FL slightly within ± FL010
                fl = max_fl
                speed = cruise_speed

            speed = max(min_speed, min(speed, max_speed))
            plan += f"{waypoint},FL{int(fl)},{int(speed)}\n"

        # Add the destination airport
        plan += f"{end_airport},{round(end_altitude, 2)}m,120"
        flight_plans.append(plan.strip())

    return flight_plans


# Example Usage
waypoints_file_path = "your_path"
airport_file_path = "your_path"
waypoints = read_waypoints(waypoints_file_path)
airports = read_airports(airport_file_path)
print("Waypoints and Airports Loaded")
flight_plans = generate_random_flight_plans(waypoints, airports)

for plan in flight_plans:
    print(plan)

output_file_path = "your_path"
with open(output_file_path, 'w') as file:
    for plan in flight_plans:
        file.write(plan + "\n")

print(f"Flight plans have been written to {output_file_path}")
