import csv
import json
import os

input_file = r'C:\Users\Hasan Can\Downloads\archive\male_players.csv'
output_file = r'C:\Users\Hasan Can\source\repos\ClubCraft\src\Services\Draft\API\Data\draft-player-pool.json'

target_leagues = {
    'Premier League': 68,
    'La Liga': 68,
    'Serie A': 68,
    'Bundesliga': 68,
    'Ligue 1': 68,
    'Super Lig': 68,
    'Liga Portugal': 68,
    'Pro League': 78
}

def map_position(pos_str):
    pos = pos_str.split(',')[0].strip()
    if pos == 'GK':
        return 'GK'
    if pos in ['CB', 'RB', 'LB', 'RWB', 'LWB']:
        return 'DEF'
    if pos in ['CM', 'CDM', 'CAM', 'RM', 'LM']:
        return 'MID'
    if pos in ['ST', 'CF', 'RW', 'LW']:
        return 'FWD'
    return 'MID' # fallback

players = []

os.makedirs(os.path.dirname(output_file), exist_ok=True)

with open(input_file, mode='r', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for row in reader:
        league = row.get('league_name', '')
        version = row.get('fifa_version', '')
        if league in target_leagues and version == '24.0':
            try:
                overall = int(row.get('overall', 0))
                if overall >= target_leagues[league]:
                    age = int(row.get('age', 0))
                    value = float(row.get('value_eur', 0) or 0)
                    if value > 0:
                        players.append({
                            'Name': row['short_name'],
                            'Position': map_position(row['player_positions']),
                            'Overall': overall,
                            'Age': age,
                            'MarketValue': value
                        })
            except Exception as e:
                pass

with open(output_file, 'w', encoding='utf-8') as f:
    json.dump(players, f, indent=2, ensure_ascii=False)

print(f"Processed {len(players)} players and saved to {output_file}")
