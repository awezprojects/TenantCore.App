// CloudClinic – Shared Ward/Room/Bed store (single source of truth for IPD + Admin)
const { useState: _wsUseState, useEffect: _wsUseEffect } = React;

// Helper: chunk a flat bed list into rooms
const chunk = (arr, size) => Array.from({ length: Math.ceil(arr.length / size) }, (_, i) => arr.slice(i * size, i * size + size));

const _genBeds = [
  { bed: 'G-01', patient: 'Sita Devi',     uhid: 'CC-20240148', days: 1, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Critical' },
  { bed: 'G-02', patient: 'Arun Mishra',   uhid: 'CC-20240155', days: 3, doctor: 'Dr. Singh',  status: 'occupied', condition: 'Stable' },
  { bed: 'G-03', patient: null, status: 'available' },
  { bed: 'G-04', patient: null, status: 'available' },
  { bed: 'G-05', patient: 'Kavitha M.',    uhid: 'CC-20240162', days: 2, doctor: 'Dr. Iyer',   status: 'occupied', condition: 'Improving' },
  { bed: 'G-06', patient: null, status: 'cleaning' },
  ...Array.from({ length: 18 }, (_, i) => ({ bed: `G-${String(i+7).padStart(2,'0')}`, patient: i%3===0 ? `Patient ${i+7}` : null, uhid: `CC-2024${200+i}`, days: (i%5)+1, doctor: 'Dr. Singh', status: i%3===0 ? 'occupied' : 'available', condition: 'Stable' })),
];

const WARDS_SEED = [
  {
    id: 'cardio', name: 'Cardiology Ward', floor: '3rd Floor', prefix: 'C', color: '#3B82F6',
    rooms: [
      { id: '301', name: 'Room 301', type: 'Private', beds: [
        { bed: 'C-01', patient: 'Vijay Gupta',   uhid: 'CC-20240135', days: 3, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Stable' },
      ]},
      { id: '302', name: 'Room 302', type: 'Semi-Private', beds: [
        { bed: 'C-02', patient: 'Ratan Lal',     uhid: 'CC-20240089', days: 7, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Improving' },
        { bed: 'C-03', patient: null, status: 'available' },
      ]},
      { id: '303', name: 'Room 303', type: 'Semi-Private', beds: [
        { bed: 'C-04', patient: 'Harish Verma',  uhid: 'CC-20240201', days: 1, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Critical' },
        { bed: 'C-05', patient: null, status: 'cleaning' },
      ]},
      { id: '304', name: 'Room 304', type: 'General (4-bed)', beds: [
        { bed: 'C-06', patient: 'Anita Bose',    uhid: 'CC-20240188', days: 4, doctor: 'Dr. Kapoor', status: 'occupied', condition: 'Stable' },
        { bed: 'C-07', patient: null, status: 'available' },
        { bed: 'C-08', patient: 'Mohan Das',     uhid: 'CC-20240212', days: 2, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Stable' },
        { bed: 'C-09', patient: 'Rekha Singh',   uhid: 'CC-20240198', days: 5, doctor: 'Dr. Kapoor', status: 'occupied', condition: 'Improving' },
      ]},
      { id: '305', name: 'Room 305', type: 'Semi-Private', beds: [
        { bed: 'C-10', patient: null, status: 'available' },
        { bed: 'C-11', patient: null, status: 'available' },
      ]},
      { id: '306', name: 'Room 306', type: 'Private', beds: [
        { bed: 'C-12', patient: 'Suresh Naik',   uhid: 'CC-20240220', days: 1, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Stable' },
      ]},
    ],
  },
  {
    id: 'general', name: 'General Ward', floor: '2nd Floor', prefix: 'G', color: '#10B981',
    rooms: chunk(_genBeds, 6).map((beds, i) => ({ id: `20${i+1}`, name: `Room 20${i+1}`, type: 'General (6-bed)', beds })),
  },
  {
    id: 'icu', name: 'ICU', floor: '4th Floor', prefix: 'ICU', color: '#EF4444',
    rooms: [
      { id: 'bayA', name: 'ICU Bay A', type: 'Critical Care', beds: [
        { bed: 'ICU-1', patient: 'Deepak Roy',   uhid: 'CC-20240230', days: 4, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Critical' },
        { bed: 'ICU-2', patient: 'Meera Ghosh',  uhid: 'CC-20240235', days: 2, doctor: 'Dr. Kapoor', status: 'occupied', condition: 'Critical' },
        { bed: 'ICU-3', patient: 'Ajit Sharma',  uhid: 'CC-20240238', days: 6, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Critical' },
        { bed: 'ICU-4', patient: null, status: 'available' },
      ]},
      { id: 'bayB', name: 'ICU Bay B', type: 'Critical Care', beds: [
        { bed: 'ICU-5', patient: 'Nita Pillai',  uhid: 'CC-20240241', days: 1, doctor: 'Dr. Roy',    status: 'occupied', condition: 'Serious' },
        { bed: 'ICU-6', patient: 'Balu Nair',    uhid: 'CC-20240244', days: 3, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Critical' },
        { bed: 'ICU-7', patient: null, status: 'available' },
        { bed: 'ICU-8', patient: 'Lata Sharma',  uhid: 'CC-20240247', days: 2, doctor: 'Dr. Kapoor', status: 'occupied', condition: 'Serious' },
      ]},
    ],
  },
  {
    id: 'maternity', name: 'Maternity Ward', floor: '1st Floor', prefix: 'M', color: '#EC4899',
    rooms: [
      { id: '101', name: 'Room 101', type: 'Private', beds: [
        { bed: 'M-01', patient: 'Priya Joshi',   uhid: 'CC-20240250', days: 2, doctor: 'Dr. Iyer',   status: 'occupied', condition: 'Stable' },
      ]},
      { id: '102', name: 'Room 102', type: 'Private', beds: [
        { bed: 'M-02', patient: 'Seema Rao',     uhid: 'CC-20240253', days: 1, doctor: 'Dr. Iyer',   status: 'occupied', condition: 'Stable' },
      ]},
      { id: '103', name: 'Room 103', type: 'Semi-Private', beds: [
        { bed: 'M-03', patient: null, status: 'available' },
        { bed: 'M-04', patient: 'Ananya Krishnan', uhid: 'CC-20240256', days: 3, doctor: 'Dr. Iyer', status: 'occupied', condition: 'Stable' },
      ]},
      { id: '104', name: 'Room 104', type: 'Delivery Suite', beds: [
        { bed: 'M-05', patient: null, status: 'available' },
        { bed: 'M-06', patient: 'Sunita Basu',   uhid: 'CC-20240259', days: 1, doctor: 'Dr. Iyer',   status: 'occupied', condition: 'Post-op' },
      ]},
      { id: '105', name: 'Room 105', type: 'Semi-Private', beds: [
        { bed: 'M-07', patient: null, status: 'cleaning' },
        { bed: 'M-08', patient: null, status: 'available' },
      ]},
    ],
  },
];

const ROOM_TYPES = ['Private', 'Semi-Private', 'General (4-bed)', 'General (6-bed)', 'Critical Care', 'Delivery Suite', 'Isolation'];
const WARD_COLORS = ['#3B82F6', '#10B981', '#EF4444', '#EC4899', '#8B5CF6', '#F59E0B', '#0891B2', '#6366F1'];

// Derived helpers
const wardBeds = (w) => w.rooms.flatMap(r => r.beds);
const bedCount = (w) => wardBeds(w).length;
const occCount = (w) => wardBeds(w).filter(b => b.status === 'occupied').length;

// ── Singleton reactive store ───────────────────────────────────────────────
const wardStore = (function () {
  let state = WARDS_SEED;
  const listeners = new Set();
  return {
    get: () => state,
    set: (updater) => {
      state = typeof updater === 'function' ? updater(state) : updater;
      listeners.forEach(fn => fn(state));
    },
    subscribe: (fn) => { listeners.add(fn); return () => listeners.delete(fn); },
  };
})();

// Hook: returns [wards, setWards] synced across every component that uses it
function useWards() {
  const [, force] = _wsUseState(0);
  _wsUseEffect(() => wardStore.subscribe(() => force(n => n + 1)), []);
  return [wardStore.get(), wardStore.set];
}

const slug = (s) => s.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '') || `id-${Date.now()}`;

Object.assign(window, {
  useWards, wardStore, wardBeds, bedCount, occCount, chunk,
  ROOM_TYPES, WARD_COLORS, slug,
});
