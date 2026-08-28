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

# ClubCraft.BuildingBlocks.Common.Enums.PlayerPosition ile birebir eslesmesi gereken
# detayli pozisyon kodlari (kaba GK/DEF/MID/FWD kategorisine indirgeme YOK artik —
# bkz. spec.md, pozisyon sistemi detaylandirma notu). CSV'nin "player_positions"
# alaninin BIRINCIL (ilk) kodu kullaniliyor.
VALID_POSITIONS = {
    'GK', 'CB', 'RB', 'LB', 'RWB', 'LWB',
    'CDM', 'CM', 'CAM', 'RM', 'LM', 'RW', 'LW', 'ST', 'CF'
}

def map_position(pos_str):
    pos = pos_str.split(',')[0].strip()
    if pos in VALID_POSITIONS:
        return pos
    # Beklenmeyen/bilinmeyen bir kod gelirse (CSV'nin farkli bir surumunde yeni
    # bir kod eklenmis olabilir) sessizce yutmak yerine acikca isaretle, boylece
    # process sonrasi raporda kac kaydin dustugu goruluyor.
    return None

players = []
skipped_unknown_position = 0

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
                        position = map_position(row['player_positions'])
                        if position is None:
                            skipped_unknown_position += 1
                            continue
                        players.append({
                            'Name': row['short_name'],
                            'Position': position,
                            'Overall': overall,
                            'Age': age,
                            'MarketValue': value
                        })
            except Exception as e:
                pass

with open(output_file, 'w', encoding='utf-8') as f:
    json.dump(players, f, indent=2, ensure_ascii=False)

print(f"Processed {len(players)} players and saved to {output_file}")
if skipped_unknown_position:
    print(f"Skipped {skipped_unknown_position} players with an unrecognized position code.")

print("\nPozisyon dagilimi:")
from collections import Counter
dist = Counter(p['Position'] for p in players)
for pos in ['GK', 'CB', 'RB', 'LB', 'RWB', 'LWB', 'CDM', 'CM', 'CAM', 'RM', 'LM', 'RW', 'LW', 'ST', 'CF']:
    print(f"  {pos}: {dist.get(pos, 0)}")
