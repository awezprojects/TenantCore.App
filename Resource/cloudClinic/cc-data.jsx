// CloudClinic – Shared data, translations, helpers
const { useState: _useState } = React;

// ── Patient visit history & vitals timeline ────────────────────────────────
const PATIENT_HISTORY = {
  'CC-20240042': { // Ramesh Kumar – Hypertensive, Diabetic
    name: 'Ramesh Kumar', age: 52, gender: 'Male', blood: 'B+',
    allergies: 'Penicillin',
    chronic: ['Hypertension', 'Type 2 Diabetes'],
    visits: [
      { date: '2024-11-15', dx: 'Hypertension – newly diagnosed', bp_sys: 158, bp_dia: 98,  sugar: 165, weight: 78,
        notes: 'New diagnosis. Lifestyle counseling. Low-salt diet advised.',
        meds: [
          { drug: 'Amlodipine', strength: '5mg',   freq: 'OD', duration: '30 days', timing: 'Morning', instructions: 'After breakfast' },
          { drug: 'Metformin',  strength: '500mg', freq: 'OD', duration: '30 days', timing: 'Morning', instructions: 'With meals' },
        ],
        investigations: ['Fasting Sugar', 'HbA1c', 'Lipid Profile'],
      },
      { date: '2025-01-20', dx: 'HTN follow-up', bp_sys: 150, bp_dia: 94, sugar: 158, weight: 77,
        notes: 'BP still elevated. Increase Amlodipine.',
        meds: [
          { drug: 'Amlodipine', strength: '10mg',  freq: 'OD', duration: '30 days', timing: 'Morning', instructions: '' },
          { drug: 'Metformin',  strength: '500mg', freq: 'BD', duration: '30 days', timing: 'Morning & Night', instructions: 'With meals' },
        ],
      },
      { date: '2025-04-10', dx: 'Routine review – add statin', bp_sys: 145, bp_dia: 92, sugar: 142, weight: 76,
        notes: 'Lipid profile abnormal. Starting statin.',
        meds: [
          { drug: 'Amlodipine',  strength: '10mg',  freq: 'OD', duration: '30 days', timing: 'Morning', instructions: '' },
          { drug: 'Metformin',   strength: '500mg', freq: 'BD', duration: '30 days', timing: 'Morning & Night', instructions: 'With meals' },
          { drug: 'Atorvastatin',strength: '10mg',  freq: 'HS', duration: '30 days', timing: 'Night',   instructions: '' },
        ],
        investigations: ['Lipid Profile (repeat in 90 days)'],
      },
      { date: '2025-08-22', dx: 'Medication adjustment', bp_sys: 138, bp_dia: 88, sugar: 135, weight: 75,
        notes: 'Responding well. Step up Atorvastatin.',
        meds: [
          { drug: 'Amlodipine',  strength: '10mg',  freq: 'OD', duration: '60 days', timing: 'Morning', instructions: '' },
          { drug: 'Metformin',   strength: '500mg', freq: 'BD', duration: '60 days', timing: 'Morning & Night', instructions: 'With meals' },
          { drug: 'Atorvastatin',strength: '20mg',  freq: 'HS', duration: '60 days', timing: 'Night',   instructions: '' },
        ],
      },
      { date: '2025-12-05', dx: 'Quarterly review', bp_sys: 132, bp_dia: 84, sugar: 128, weight: 74,
        notes: 'Stable. Continue same regimen.',
        meds: [
          { drug: 'Amlodipine',  strength: '10mg',  freq: 'OD', duration: '90 days', timing: 'Morning', instructions: '' },
          { drug: 'Metformin',   strength: '500mg', freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: 'With meals' },
          { drug: 'Atorvastatin',strength: '20mg',  freq: 'HS', duration: '90 days', timing: 'Night',   instructions: '' },
        ],
      },
      { date: '2026-03-18', dx: 'Stable – maintenance', bp_sys: 128, bp_dia: 82, sugar: 122, weight: 74,
        notes: 'Excellent control. 30-min walk daily.',
        meds: [
          { drug: 'Amlodipine',  strength: '5mg',   freq: 'OD', duration: '90 days', timing: 'Morning', instructions: 'After breakfast' },
          { drug: 'Metformin',   strength: '500mg', freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: 'With meals' },
          { drug: 'Atorvastatin',strength: '20mg',  freq: 'HS', duration: '90 days', timing: 'Night',   instructions: '' },
        ],
        investigations: ['HbA1c', 'KFT'],
      },
    ],
  },
  'CC-20240078': {
    name: 'Sunita Verma', age: 34, gender: 'Female', blood: 'A+',
    allergies: 'None known', chronic: ['Mild Hypertension'],
    visits: [
      { date: '2025-06-10', dx: 'Stage 1 Hypertension', bp_sys: 142, bp_dia: 90, sugar: 98, weight: 62,
        notes: 'New diagnosis. Lifestyle first, plus low-dose ARB.',
        meds: [
          { drug: 'Losartan', strength: '25mg', freq: 'OD', duration: '30 days', timing: 'Morning', instructions: '' },
        ],
        investigations: ['BP monitoring at home', 'KFT', 'Urine routine'],
      },
      { date: '2025-09-12', dx: 'Follow-up – partial response', bp_sys: 135, bp_dia: 86, sugar: 95, weight: 61,
        notes: 'BP improving but not at goal. Increase dose.',
        meds: [
          { drug: 'Losartan', strength: '50mg', freq: 'OD', duration: '60 days', timing: 'Morning', instructions: '' },
        ],
      },
      { date: '2026-01-08', dx: 'Controlled', bp_sys: 128, bp_dia: 82, sugar: 92, weight: 60,
        notes: 'At target. Aerobic exercise 3x/week.',
        meds: [
          { drug: 'Losartan', strength: '50mg', freq: 'OD', duration: '90 days', timing: 'Morning', instructions: '' },
        ],
      },
    ],
  },
  'CC-20240101': {
    name: 'Anil Sharma', age: 61, gender: 'Male', blood: 'O+',
    allergies: 'Sulfa drugs', chronic: ['CAD', 'Diabetes', 'Hypertension'],
    visits: [
      { date: '2024-09-05', dx: 'CAD baseline – pre-PCI', bp_sys: 165, bp_dia: 100, sugar: 195, weight: 82,
        notes: 'TMT positive. PCI planned.',
        meds: [
          { drug: 'Aspirin',     strength: '75mg',  freq: 'OD', duration: '30 days', timing: 'Morning', instructions: 'After breakfast' },
          { drug: 'Atorvastatin',strength: '40mg',  freq: 'HS', duration: '30 days', timing: 'Night',   instructions: '' },
          { drug: 'Metformin',   strength: '500mg', freq: 'BD', duration: '30 days', timing: 'Morning & Night', instructions: 'With meals' },
        ],
        investigations: ['Coronary Angiography', '2D Echo', 'HbA1c'],
      },
      { date: '2024-12-15', dx: 'Post-PCI day 14', bp_sys: 150, bp_dia: 92, sugar: 178, weight: 80,
        notes: 'Successful PCI to LAD. DAPT for 12 months.',
        meds: [
          { drug: 'Aspirin',     strength: '75mg',  freq: 'OD', duration: '30 days', timing: 'Morning', instructions: 'After breakfast' },
          { drug: 'Clopidogrel', strength: '75mg',  freq: 'OD', duration: '30 days', timing: 'Morning', instructions: '' },
          { drug: 'Metoprolol',  strength: '25mg',  freq: 'BD', duration: '30 days', timing: 'Morning & Night', instructions: '' },
          { drug: 'Atorvastatin',strength: '40mg',  freq: 'HS', duration: '30 days', timing: 'Night',   instructions: '' },
          { drug: 'Metformin',   strength: '500mg', freq: 'BD', duration: '30 days', timing: 'Morning & Night', instructions: 'With meals' },
        ],
      },
      { date: '2025-03-20', dx: 'Recovery – stable', bp_sys: 140, bp_dia: 88, sugar: 162, weight: 79,
        notes: 'No chest pain. Cardiac rehab ongoing.',
        meds: [
          { drug: 'Aspirin',     strength: '75mg',  freq: 'OD', duration: '90 days', timing: 'Morning', instructions: '' },
          { drug: 'Clopidogrel', strength: '75mg',  freq: 'OD', duration: '90 days', timing: 'Morning', instructions: '' },
          { drug: 'Metoprolol',  strength: '50mg',  freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: '' },
          { drug: 'Atorvastatin',strength: '40mg',  freq: 'HS', duration: '90 days', timing: 'Night',   instructions: '' },
          { drug: 'Metformin',   strength: '500mg', freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: 'With meals' },
        ],
      },
      { date: '2025-07-08', dx: 'Stable angina f/u', bp_sys: 138, bp_dia: 86, sugar: 155, weight: 78,
        notes: 'Continue regimen.',
        meds: [
          { drug: 'Aspirin',     strength: '75mg',  freq: 'OD', duration: '90 days', timing: 'Morning', instructions: '' },
          { drug: 'Clopidogrel', strength: '75mg',  freq: 'OD', duration: '90 days', timing: 'Morning', instructions: '' },
          { drug: 'Metoprolol',  strength: '50mg',  freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: '' },
          { drug: 'Atorvastatin',strength: '40mg',  freq: 'HS', duration: '90 days', timing: 'Night',   instructions: '' },
          { drug: 'Metformin',   strength: '500mg', freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: 'With meals' },
        ],
      },
      { date: '2025-11-22', dx: 'Annual review', bp_sys: 134, bp_dia: 84, sugar: 145, weight: 77,
        notes: '1-year post-PCI. Step down to single antiplatelet.',
        meds: [
          { drug: 'Aspirin',     strength: '75mg',  freq: 'OD', duration: '90 days', timing: 'Morning', instructions: '' },
          { drug: 'Metoprolol',  strength: '50mg',  freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: '' },
          { drug: 'Atorvastatin',strength: '40mg',  freq: 'HS', duration: '90 days', timing: 'Night',   instructions: '' },
          { drug: 'Metformin',   strength: '500mg', freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: 'With meals' },
        ],
        investigations: ['2D Echo', 'Lipid Profile', 'HbA1c'],
      },
      { date: '2026-04-05', dx: 'Routine', bp_sys: 132, bp_dia: 82, sugar: 138, weight: 77,
        notes: 'Stable. Continue regimen.',
        meds: [
          { drug: 'Aspirin',     strength: '75mg',  freq: 'OD', duration: '90 days', timing: 'Morning', instructions: '' },
          { drug: 'Metoprolol',  strength: '50mg',  freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: '' },
          { drug: 'Atorvastatin',strength: '40mg',  freq: 'HS', duration: '90 days', timing: 'Night',   instructions: '' },
          { drug: 'Metformin',   strength: '500mg', freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: 'With meals' },
        ],
      },
    ],
  },
};

// ── Today's appointments (for Doctor dashboard) ────────────────────────────
const TODAY_APPOINTMENTS = [
  { id: 'A001', time: '09:00', uhid: 'CC-20240042', name: 'Ramesh Kumar',  age: 52, gender: 'M', reason: 'BP/Sugar review',     status: 'completed' },
  { id: 'A002', time: '09:30', uhid: 'CC-20240078', name: 'Sunita Verma',  age: 34, gender: 'F', reason: 'Hypertension f/u',    status: 'completed' },
  { id: 'A003', time: '10:00', uhid: 'CC-20240101', name: 'Anil Sharma',   age: 61, gender: 'M', reason: 'Chest discomfort',    status: 'completed' },
  { id: 'A004', time: '10:30', uhid: 'CC-20240115', name: 'Meena Patel',   age: 28, gender: 'F', reason: 'Annual checkup',      status: 'completed' },
  { id: 'A005', time: '11:00', uhid: 'CC-20240122', name: 'Geeta Nair',    age: 38, gender: 'F', reason: 'Palpitations',        status: 'completed' },
  { id: 'A006', time: '11:30', uhid: 'CC-20240119', name: 'Ravi Singh',    age: 45, gender: 'M', reason: 'ECG review',          status: 'in-progress' },
  { id: 'A007', time: '12:00', uhid: 'CC-20240135', name: 'Vijay Gupta',   age: 55, gender: 'M', reason: 'Knee pain',           status: 'waiting' },
  { id: 'A008', time: '15:00', uhid: 'CC-20240148', name: 'Priya Joshi',   age: 29, gender: 'F', reason: 'Fever',               status: 'waiting' },
  { id: 'A009', time: '15:30', uhid: 'CC-20240155', name: 'Arun Mishra',   age: 47, gender: 'M', reason: 'Back pain',           status: 'waiting' },
  { id: 'A010', time: '16:00', uhid: 'CC-20240162', name: 'Kavitha M.',    age: 41, gender: 'F', reason: 'Thyroid review',      status: 'waiting' },
];

// ── Prescriptions DB (visited patients with full Rx) ───────────────────────
const PRESCRIPTIONS_DB = [
  {
    id: 'RX-2026-0512-001', uhid: 'CC-20240042', name: 'Ramesh Kumar', age: 52, gender: 'M',
    date: '2026-05-12', time: '09:15',
    diagnosis: 'Essential Hypertension, Type 2 DM – well controlled',
    vitals: { bp: '128/82', pulse: '74', temp: '98.4', weight: '74', spo2: '98', rr: '16', sugar: '122' },
    meds: [
      { drug: 'Amlodipine',  form: 'Tablet', strength: '5mg',  qty: 1, freq: 'OD', duration: '30 days', timing: 'Morning',         instructions: 'After breakfast' },
      { drug: 'Metformin',   form: 'Tablet', strength: '500mg', qty: 1, freq: 'BD', duration: '30 days', timing: 'Morning & Night', instructions: 'With meals' },
      { drug: 'Atorvastatin',form: 'Tablet', strength: '20mg', qty: 1, freq: 'HS', duration: '30 days', timing: 'Night',           instructions: '' },
    ],
    investigations: ['Lipid Profile', 'HbA1c', 'Kidney Function Test'],
    followup: '2026-06-12',
    notes: 'Continue low-salt, low-sugar diet. 30-min walk daily.',
  },
  {
    id: 'RX-2026-0512-002', uhid: 'CC-20240078', name: 'Sunita Verma', age: 34, gender: 'F',
    date: '2026-05-12', time: '09:45',
    diagnosis: 'Stage 1 Hypertension – controlled',
    vitals: { bp: '128/82', pulse: '72', temp: '98.2', weight: '60', spo2: '99', rr: '14', sugar: '92' },
    meds: [
      { drug: 'Losartan',    form: 'Tablet', strength: '50mg', qty: 1, freq: 'OD', duration: '30 days', timing: 'Morning', instructions: '' },
    ],
    investigations: ['BP monitoring at home'],
    followup: '2026-07-12',
    notes: 'Reduce caffeine. Aerobic exercise 3x/week.',
  },
  {
    id: 'RX-2026-0512-003', uhid: 'CC-20240101', name: 'Anil Sharma', age: 61, gender: 'M',
    date: '2026-05-12', time: '10:20',
    diagnosis: 'CAD post-PCI, Diabetes, HTN',
    vitals: { bp: '132/82', pulse: '78', temp: '98.6', weight: '77', spo2: '97', rr: '18', sugar: '138' },
    meds: [
      { drug: 'Aspirin',     form: 'Tablet', strength: '75mg', qty: 1, freq: 'OD', duration: '90 days', timing: 'Morning',         instructions: 'After breakfast' },
      { drug: 'Clopidogrel', form: 'Tablet', strength: '75mg', qty: 1, freq: 'OD', duration: '90 days', timing: 'Morning',         instructions: '' },
      { drug: 'Metoprolol',  form: 'Tablet', strength: '50mg', qty: 1, freq: 'BD', duration: '90 days', timing: 'Morning & Night', instructions: '' },
      { drug: 'Atorvastatin',form: 'Tablet', strength: '40mg', qty: 1, freq: 'HS', duration: '90 days', timing: 'Night',           instructions: '' },
    ],
    investigations: ['2D Echo', 'Lipid Profile', 'HbA1c'],
    followup: '2026-08-12',
    notes: 'No strenuous activity. Cardiac rehab program advised.',
  },
  {
    id: 'RX-2026-0512-004', uhid: 'CC-20240115', name: 'Meena Patel', age: 28, gender: 'F',
    date: '2026-05-12', time: '10:45',
    diagnosis: 'Iron deficiency anaemia (mild)',
    vitals: { bp: '110/72', pulse: '82', temp: '98.4', weight: '54', spo2: '99', rr: '16', sugar: '88' },
    meds: [
      { drug: 'Ferrous Sulfate', form: 'Tablet', strength: '200mg', qty: 1, freq: 'BD', duration: '60 days', timing: 'Morning & Night', instructions: 'After meals' },
      { drug: 'Folic Acid',      form: 'Tablet', strength: '5mg',   qty: 1, freq: 'OD', duration: '60 days', timing: 'Morning',         instructions: '' },
    ],
    investigations: ['CBC (after 30 days)'],
    followup: '2026-06-15',
    notes: 'Iron-rich diet: leafy greens, jaggery, dates.',
  },
  {
    id: 'RX-2026-0512-005', uhid: 'CC-20240122', name: 'Geeta Nair', age: 38, gender: 'F',
    date: '2026-05-12', time: '11:15',
    diagnosis: 'Anxiety-induced palpitations, no structural heart disease',
    vitals: { bp: '118/76', pulse: '88', temp: '98.5', weight: '58', spo2: '99', rr: '18', sugar: '94' },
    meds: [
      { drug: 'Propranolol', form: 'Tablet', strength: '10mg', qty: 1, freq: 'BD', duration: '21 days', timing: 'Morning & Night', instructions: 'SOS if palpitations' },
    ],
    investigations: ['Holter monitoring', 'TSH'],
    followup: '2026-06-05',
    notes: 'Breathing exercises, reduce caffeine.',
  },
  // Older prescriptions
  {
    id: 'RX-2026-0510-002', uhid: 'CC-20240042', name: 'Ramesh Kumar', age: 52, gender: 'M',
    date: '2026-05-10', time: '10:00',
    diagnosis: 'HTN, DM – review',
    vitals: { bp: '130/84', pulse: '76', temp: '98.4', weight: '75', spo2: '98', rr: '16', sugar: '128' },
    meds: [
      { drug: 'Amlodipine', form: 'Tablet', strength: '5mg', qty: 1, freq: 'OD', duration: '30 days', timing: 'Morning', instructions: '' },
    ],
    investigations: ['Fasting Sugar'], followup: '2026-06-10', notes: '',
  },
  {
    id: 'RX-2026-0508-001', uhid: 'CC-20240115', name: 'Meena Patel', age: 28, gender: 'F',
    date: '2026-05-08', time: '11:30',
    diagnosis: 'Iron deficiency – baseline',
    vitals: { bp: '108/70', pulse: '84', temp: '98.4', weight: '54', spo2: '99', rr: '16', sugar: '88' },
    meds: [
      { drug: 'Ferrous Sulfate', form: 'Tablet', strength: '200mg', qty: 1, freq: 'OD', duration: '30 days', timing: 'Morning', instructions: 'After meals' },
    ],
    investigations: ['CBC', 'Iron studies'], followup: '2026-05-12', notes: '',
  },
  {
    id: 'RX-2026-0505-001', uhid: 'CC-20240101', name: 'Anil Sharma', age: 61, gender: 'M',
    date: '2026-05-05', time: '09:30',
    diagnosis: 'CAD review',
    vitals: { bp: '134/86', pulse: '80', temp: '98.6', weight: '78', spo2: '97', rr: '18', sugar: '142' },
    meds: [
      { drug: 'Aspirin', form: 'Tablet', strength: '75mg', qty: 1, freq: 'OD', duration: '30 days', timing: 'Morning', instructions: '' },
    ],
    investigations: ['ECG'], followup: '2026-05-12', notes: '',
  },
];

// ── Investigations Master ──────────────────────────────────────────────────
const INVESTIGATIONS_MASTER = {
  'Blood Tests': ['CBC', 'ESR', 'Blood Sugar (Fasting)', 'Blood Sugar (PP)', 'HbA1c', 'Lipid Profile', 'LFT', 'KFT', 'Thyroid Profile (TSH, T3, T4)', 'Vitamin D3', 'Vitamin B12', 'Iron Studies', 'CRP', 'Uric Acid'],
  'Urine':       ['Urine Routine', 'Urine Culture & Sensitivity', 'Urine Microalbumin'],
  'Cardiac':     ['ECG', '2D Echo', 'TMT (Stress Test)', 'Holter Monitoring', 'Troponin-I'],
  'Radiology':   ['Chest X-Ray', 'Abdominal Ultrasound', 'CT Brain', 'CT Chest', 'MRI Brain', 'X-Ray Spine'],
  'Other':       ['EEG', 'Pulmonary Function Test', 'Endoscopy', 'Colonoscopy', 'Pap Smear'],
};

// ── Drug Form Categories ───────────────────────────────────────────────────
const DRUG_FORM_TYPES = ['Tablet', 'Capsule', 'Syrup', 'Injection', 'Eye Drop', 'Ear Drop', 'Nasal Drop', 'Cream', 'Ointment', 'Inhaler', 'Powder', 'Suppository'];

// ── Frequency presets (user-friendly) ──────────────────────────────────────
const FREQUENCY_PRESETS = [
  { code: 'OD',  label: 'Once Daily',         shortLabel: '1×',     pattern: '1-0-0', times: 1 },
  { code: 'BD',  label: 'Twice Daily',        shortLabel: '2×',     pattern: '1-0-1', times: 2 },
  { code: 'TDS', label: 'Three Times Daily',  shortLabel: '3×',     pattern: '1-1-1', times: 3 },
  { code: 'QID', label: 'Four Times Daily',   shortLabel: '4×',     pattern: '1-1-1-1', times: 4 },
  { code: 'HS',  label: 'At Bedtime',         shortLabel: 'Night',  pattern: '0-0-1', times: 1 },
  { code: 'SOS', label: 'As Needed',          shortLabel: 'PRN',    pattern: 'When required', times: 0 },
];

// ── Instruction Templates (predefined, picker-style) ───────────────────────
const INSTRUCTION_TEMPLATES = {
  'When to take': [
    'Before meals',
    'After meals',
    'On empty stomach',
    '30 minutes before food',
    '1 hour after meals',
    'With breakfast',
    'At bedtime only',
  ],
  'How to take': [
    'With a full glass of water',
    'With milk',
    'Chew before swallowing',
    'Do not crush or chew',
    'Place under the tongue',
    'Dissolve in water',
    'Shake well before use',
  ],
  'Cautions': [
    'Avoid alcohol',
    'Avoid driving after dose',
    'Avoid sunlight exposure',
    'Avoid grapefruit juice',
    'Do not take with antacids',
    'Stop if rash appears',
  ],
  'Storage': [
    'Refrigerate (2–8 °C)',
    'Keep in a cool, dry place',
    'Protect from light',
    'Do not freeze',
  ],
};

// ── Multi-language Translations ────────────────────────────────────────────
const TRANSLATIONS = {
  en: {
    // Form-specific verbs
    take: 'Take', apply: 'Apply', instill: 'Instill', inhale: 'Inhale', inject: 'Inject',
    // Units
    tablet: 'tablet', tablets: 'tablets', capsule: 'capsule', capsules: 'capsules',
    ml: 'ml', drop: 'drop', drops: 'drops', puff: 'puff', puffs: 'puffs',
    fingertipUnit: 'fingertip unit', amount: 'thin layer',
    // Frequencies
    OD: 'once a day', BD: 'twice a day', TDS: 'three times a day', QID: 'four times a day',
    HS: 'at bedtime', SOS: 'when required', QH: 'every hour', Q4H: 'every 4 hours', Q6H: 'every 6 hours',
    OW: 'once weekly', BW: 'twice weekly',
    // Timing
    morning: 'in the morning', afternoon: 'in the afternoon', evening: 'in the evening', night: 'at night',
    beforeMeals: 'before meals', afterMeals: 'after meals', emptyStomach: 'on empty stomach',
    // Sites
    affectedArea: 'on the affected area', bothEyes: 'in both eyes', affectedEye: 'in the affected eye',
    affectedEar: 'in the affected ear', nostrils: 'in each nostril',
    // Common
    for: 'for', days: 'days', weeks: 'weeks',
    in: '', // grammar helper
  },
  hi: {
    take: 'लें', apply: 'लगाएं', instill: 'डालें', inhale: 'सूँघें', inject: 'इंजेक्ट करें',
    tablet: 'गोली', tablets: 'गोलियाँ', capsule: 'कैप्सूल', capsules: 'कैप्सूल',
    ml: 'मि.ली.', drop: 'बूँद', drops: 'बूँदें', puff: 'पफ', puffs: 'पफ',
    fingertipUnit: 'अँगुली के अग्रभाग बराबर', amount: 'पतली परत',
    OD: 'दिन में एक बार', BD: 'दिन में दो बार', TDS: 'दिन में तीन बार', QID: 'दिन में चार बार',
    HS: 'सोते समय', SOS: 'ज़रूरत पड़ने पर', QH: 'हर घंटे', Q4H: 'हर 4 घंटे', Q6H: 'हर 6 घंटे',
    OW: 'सप्ताह में एक बार', BW: 'सप्ताह में दो बार',
    morning: 'सुबह', afternoon: 'दोपहर में', evening: 'शाम को', night: 'रात को',
    beforeMeals: 'भोजन से पहले', afterMeals: 'भोजन के बाद', emptyStomach: 'खाली पेट',
    affectedArea: 'प्रभावित जगह पर', bothEyes: 'दोनों आँखों में', affectedEye: 'प्रभावित आँख में',
    affectedEar: 'प्रभावित कान में', nostrils: 'प्रत्येक नथुने में',
    for: 'के लिए', days: 'दिन', weeks: 'सप्ताह',
  },
  mr: {
    take: 'घ्या', apply: 'लावा', instill: 'टाका', inhale: 'श्वासाने घ्या', inject: 'इंजेक्शन द्या',
    tablet: 'गोळी', tablets: 'गोळ्या', capsule: 'कॅप्सूल', capsules: 'कॅप्सूल',
    ml: 'मि.ली.', drop: 'थेंब', drops: 'थेंब', puff: 'पफ', puffs: 'पफ',
    fingertipUnit: 'बोटाच्या टोकाएवढे', amount: 'पातळ थर',
    OD: 'दिवसातून एकदा', BD: 'दिवसातून दोनदा', TDS: 'दिवसातून तीनदा', QID: 'दिवसातून चार वेळा',
    HS: 'झोपण्यापूर्वी', SOS: 'आवश्यक असल्यास', QH: 'दर तासाला', Q4H: 'दर ४ तासांनी', Q6H: 'दर ६ तासांनी',
    OW: 'आठवड्यातून एकदा', BW: 'आठवड्यातून दोनदा',
    morning: 'सकाळी', afternoon: 'दुपारी', evening: 'संध्याकाळी', night: 'रात्री',
    beforeMeals: 'जेवणापूर्वी', afterMeals: 'जेवणानंतर', emptyStomach: 'रिकाम्या पोटी',
    affectedArea: 'प्रभावित भागावर', bothEyes: 'दोन्ही डोळ्यांत', affectedEye: 'प्रभावित डोळ्यात',
    affectedEar: 'प्रभावित कानात', nostrils: 'प्रत्येक नाकपुडीत',
    for: 'साठी', days: 'दिवस', weeks: 'आठवडे',
  },
};

// ── Auto-generate prescription remark text ─────────────────────────────────
function generateRemark(med, lang = 'en') {
  const t = TRANSLATIONS[lang] || TRANSLATIONS.en;
  const qty = med.qty || 1;
  const form = (med.form || 'Tablet').toLowerCase();
  const freq = med.freq || 'OD';
  // Normalise duration: accepts a number (days), or a string like "30 days"
  let durationTxt = '';
  if (med.duration != null && med.duration !== '') {
    if (typeof med.duration === 'number') {
      durationTxt = `${med.duration} ${t.days}`;
    } else {
      const m = String(med.duration).match(/(\d+)/);
      if (m) durationTxt = `${m[1]} ${t.days}`;
      else durationTxt = med.duration;
    }
  }

  let verb = t.take, unit = t.tablet, site = '';

  if (form === 'tablet')        { verb = t.take;    unit = qty > 1 ? t.tablets  : t.tablet; }
  else if (form === 'capsule')  { verb = t.take;    unit = qty > 1 ? t.capsules : t.capsule; }
  else if (form === 'syrup' || form === 'liquid')   { verb = t.take; unit = t.ml; }
  else if (form === 'injection'){ verb = t.inject;  unit = t.ml; }
  else if (form === 'eye drop') { verb = t.instill; unit = qty > 1 ? t.drops : t.drop; site = ' ' + t.bothEyes; }
  else if (form === 'ear drop') { verb = t.instill; unit = qty > 1 ? t.drops : t.drop; site = ' ' + t.affectedEar; }
  else if (form === 'nasal drop'){verb = t.instill; unit = qty > 1 ? t.drops : t.drop; site = ' ' + t.nostrils; }
  else if (form === 'cream' || form === 'ointment') { verb = t.apply; unit = t.amount; site = ' ' + t.affectedArea; }
  else if (form === 'inhaler')  { verb = t.inhale;  unit = qty > 1 ? t.puffs : t.puff; }
  else if (form === 'powder')   { verb = t.take;    unit = '1 sachet'; }

  const freqTxt = t[freq] || freq;
  const parts = [`${verb} ${qty} ${unit}${site}`, freqTxt];
  if (durationTxt) parts.push(`${t.for} ${durationTxt}`);

  return parts.filter(Boolean).join(', ');
}

Object.assign(window, {
  PATIENT_HISTORY, TODAY_APPOINTMENTS, PRESCRIPTIONS_DB,
  INVESTIGATIONS_MASTER, DRUG_FORM_TYPES, FREQUENCY_PRESETS, INSTRUCTION_TEMPLATES,
  TRANSLATIONS, generateRemark,
});
