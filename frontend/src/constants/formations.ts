// Draft ekranı (App.tsx) ve Sezon Dashboard (SeasonDashboard.tsx) AYNI formasyon
// tanımını paylaşmalı — backend'de de tek bir Club.Formation alanı var
// (bkz. UpdateFormationCommand, PUT /api/clubs/{clubId}/formation). Bu sabit
// iki yerde ayrı ayrı tanımlanıp driftlemesin diye tek dosyada tutuluyor
// (tıpkı eski tek-formasyonun iki dosyada kopyalanmış olmasının kendisinin bir
// bakım riski olduğu gibi — bkz. spec.md, pozisyon sistemi detaylandırma notu).

export interface FormationSlot { id: string; label: string; top: string; left: string; }

// Detayli pozisyon kodu (bkz. BuildingBlocks.Common.Enums.PlayerPosition) -> renk grubu.
// .cc-pos-badge / .cc-pos-pill CSS'i sadece GK/DEF/MID/FWD renklerini taniyor; rozet
// METNİ hala tam detayli kodu (orn. "CDM") gosteriyor, sadece RENGİ bu gruba gore seciliyor.
export const POSITION_GROUP: Record<string, string> = {
  GK: 'GK',
  CB: 'DEF', RB: 'DEF', LB: 'DEF', RWB: 'DEF', LWB: 'DEF',
  CDM: 'MID', CM: 'MID', CAM: 'MID', RM: 'MID', LM: 'MID',
  RW: 'FWD', LW: 'FWD', ST: 'FWD', CF: 'FWD',
};

// Kullanicinin secebilecegi 4 formasyon — her biri farkli slot sayisi/dagilimi
// tasiyor. Formasyon degisince lineup sifirlaniyor (bkz. Club.UpdateFormation)
// cunku eski slot ID'leri yeni formasyonda anlamli olmayabilir.
export const FORMATIONS: Record<string, FormationSlot[]> = {
  '4-4-2': [
    { id: 'GK', label: 'GK', top: '90%', left: '50%' },
    { id: 'LB', label: 'LB', top: '72%', left: '20%' },
    { id: 'CB1', label: 'CB', top: '75%', left: '40%' },
    { id: 'CB2', label: 'CB', top: '75%', left: '60%' },
    { id: 'RB', label: 'RB', top: '72%', left: '80%' },
    { id: 'LM', label: 'LM', top: '45%', left: '20%' },
    { id: 'CM1', label: 'CM', top: '48%', left: '40%' },
    { id: 'CM2', label: 'CM', top: '48%', left: '60%' },
    { id: 'RM', label: 'RM', top: '45%', left: '80%' },
    { id: 'ST1', label: 'ST', top: '20%', left: '35%' },
    { id: 'ST2', label: 'ST', top: '20%', left: '65%' },
  ],
  '4-3-3': [
    { id: 'GK', label: 'GK', top: '90%', left: '50%' },
    { id: 'LB', label: 'LB', top: '72%', left: '20%' },
    { id: 'CB1', label: 'CB', top: '75%', left: '40%' },
    { id: 'CB2', label: 'CB', top: '75%', left: '60%' },
    { id: 'RB', label: 'RB', top: '72%', left: '80%' },
    { id: 'CDM', label: 'CDM', top: '58%', left: '50%' },
    { id: 'CM1', label: 'CM', top: '48%', left: '30%' },
    { id: 'CM2', label: 'CM', top: '48%', left: '70%' },
    { id: 'LW', label: 'LW', top: '22%', left: '18%' },
    { id: 'ST', label: 'ST', top: '18%', left: '50%' },
    { id: 'RW', label: 'RW', top: '22%', left: '82%' },
  ],
  '4-2-3-1': [
    { id: 'GK', label: 'GK', top: '90%', left: '50%' },
    { id: 'LB', label: 'LB', top: '72%', left: '20%' },
    { id: 'CB1', label: 'CB', top: '75%', left: '40%' },
    { id: 'CB2', label: 'CB', top: '75%', left: '60%' },
    { id: 'RB', label: 'RB', top: '72%', left: '80%' },
    { id: 'CDM1', label: 'CDM', top: '58%', left: '38%' },
    { id: 'CDM2', label: 'CDM', top: '58%', left: '62%' },
    { id: 'LW', label: 'LW', top: '36%', left: '18%' },
    { id: 'CAM', label: 'CAM', top: '33%', left: '50%' },
    { id: 'RW', label: 'RW', top: '36%', left: '82%' },
    { id: 'ST', label: 'ST', top: '16%', left: '50%' },
  ],
  '3-5-2': [
    { id: 'GK', label: 'GK', top: '90%', left: '50%' },
    { id: 'CB1', label: 'CB', top: '75%', left: '30%' },
    { id: 'CB2', label: 'CB', top: '78%', left: '50%' },
    { id: 'CB3', label: 'CB', top: '75%', left: '70%' },
    { id: 'LWB', label: 'LWB', top: '48%', left: '10%' },
    { id: 'CDM', label: 'CDM', top: '55%', left: '38%' },
    { id: 'CM', label: 'CM', top: '52%', left: '50%' },
    { id: 'CAM', label: 'CAM', top: '55%', left: '62%' },
    { id: 'RWB', label: 'RWB', top: '48%', left: '90%' },
    { id: 'ST1', label: 'ST', top: '20%', left: '38%' },
    { id: 'ST2', label: 'ST', top: '20%', left: '62%' },
  ],
};

export const FORMATION_NAMES = Object.keys(FORMATIONS);
