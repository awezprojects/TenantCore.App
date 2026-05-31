// CloudClinic – IPD & Bed Management
const { useState } = React;

const WARDS = [
  {
    id: 'cardio', name: 'Cardiology Ward', floor: '3rd Floor', total: 12, color: '#3B82F6',
    beds: [
      { bed: 'C-01', patient: 'Vijay Gupta',   uhid: 'CC-20240135', days: 3, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Stable' },
      { bed: 'C-02', patient: 'Ratan Lal',     uhid: 'CC-20240089', days: 7, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Improving' },
      { bed: 'C-03', patient: null, status: 'available' },
      { bed: 'C-04', patient: 'Harish Verma',  uhid: 'CC-20240201', days: 1, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Critical' },
      { bed: 'C-05', patient: null, status: 'cleaning' },
      { bed: 'C-06', patient: 'Anita Bose',    uhid: 'CC-20240188', days: 4, doctor: 'Dr. Kapoor', status: 'occupied', condition: 'Stable' },
      { bed: 'C-07', patient: null, status: 'available' },
      { bed: 'C-08', patient: 'Mohan Das',     uhid: 'CC-20240212', days: 2, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Stable' },
      { bed: 'C-09', patient: 'Rekha Singh',   uhid: 'CC-20240198', days: 5, doctor: 'Dr. Kapoor', status: 'occupied', condition: 'Improving' },
      { bed: 'C-10', patient: null, status: 'available' },
      { bed: 'C-11', patient: null, status: 'available' },
      { bed: 'C-12', patient: 'Suresh Naik',   uhid: 'CC-20240220', days: 1, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Stable' },
    ],
  },
  {
    id: 'general', name: 'General Ward', floor: '2nd Floor', total: 24, color: '#10B981',
    beds: [
      { bed: 'G-01', patient: 'Sita Devi',     uhid: 'CC-20240148', days: 1, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Critical' },
      { bed: 'G-02', patient: 'Arun Mishra',   uhid: 'CC-20240155', days: 3, doctor: 'Dr. Singh',  status: 'occupied', condition: 'Stable' },
      { bed: 'G-03', patient: null, status: 'available' },
      { bed: 'G-04', patient: null, status: 'available' },
      { bed: 'G-05', patient: 'Kavitha M.',    uhid: 'CC-20240162', days: 2, doctor: 'Dr. Iyer',   status: 'occupied', condition: 'Improving' },
      { bed: 'G-06', patient: null, status: 'cleaning' },
      ...Array.from({ length: 18 }, (_, i) => ({ bed: `G-${String(i+7).padStart(2,'0')}`, patient: i%3===0 ? `Patient ${i+7}` : null, uhid: `CC-2024${200+i}`, days: (i%5)+1, doctor: 'Dr. Singh', status: i%3===0 ? 'occupied' : 'available', condition: 'Stable' })),
    ],
  },
  {
    id: 'icu', name: 'ICU', floor: '4th Floor', total: 8, color: '#EF4444',
    beds: [
      { bed: 'ICU-1', patient: 'Deepak Roy',   uhid: 'CC-20240230', days: 4, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Critical' },
      { bed: 'ICU-2', patient: 'Meera Ghosh',  uhid: 'CC-20240235', days: 2, doctor: 'Dr. Kapoor', status: 'occupied', condition: 'Critical' },
      { bed: 'ICU-3', patient: 'Ajit Sharma',  uhid: 'CC-20240238', days: 6, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Critical' },
      { bed: 'ICU-4', patient: null, status: 'available' },
      { bed: 'ICU-5', patient: 'Nita Pillai',  uhid: 'CC-20240241', days: 1, doctor: 'Dr. Roy',    status: 'occupied', condition: 'Serious' },
      { bed: 'ICU-6', patient: 'Balu Nair',    uhid: 'CC-20240244', days: 3, doctor: 'Dr. Mehta',  status: 'occupied', condition: 'Critical' },
      { bed: 'ICU-7', patient: null, status: 'available' },
      { bed: 'ICU-8', patient: 'Lata Sharma',  uhid: 'CC-20240247', days: 2, doctor: 'Dr. Kapoor', status: 'occupied', condition: 'Serious' },
    ],
  },
  {
    id: 'maternity', name: 'Maternity Ward', floor: '1st Floor', total: 8, color: '#EC4899',
    beds: [
      { bed: 'M-01', patient: 'Priya Joshi',   uhid: 'CC-20240250', days: 2, doctor: 'Dr. Iyer',   status: 'occupied', condition: 'Stable' },
      { bed: 'M-02', patient: 'Seema Rao',     uhid: 'CC-20240253', days: 1, doctor: 'Dr. Iyer',   status: 'occupied', condition: 'Stable' },
      { bed: 'M-03', patient: null, status: 'available' },
      { bed: 'M-04', patient: 'Ananya Krishnan', uhid: 'CC-20240256', days: 3, doctor: 'Dr. Iyer', status: 'occupied', condition: 'Stable' },
      { bed: 'M-05', patient: null, status: 'available' },
      { bed: 'M-06', patient: 'Sunita Basu',   uhid: 'CC-20240259', days: 1, doctor: 'Dr. Iyer',   status: 'occupied', condition: 'Post-op' },
      { bed: 'M-07', patient: null, status: 'cleaning' },
      { bed: 'M-08', patient: null, status: 'available' },
    ],
  },
];

const COND_COLOR = {
  Stable:     { c: CC.success, bg: '#ECFDF5' },
  Improving:  { c: '#0891B2',  bg: '#ECFEFF' },
  Serious:    { c: CC.warning, bg: '#FFFBEB' },
  Critical:   { c: CC.error,   bg: '#FEF2F2' },
  'Post-op':  { c: '#7C3AED',  bg: '#F5F3FF' },
};

function IPDModule() {
  const [wards, setWards]         = useState(WARDS);
  const [activeWard, setActiveWard] = useState('cardio');
  const [selectedBed, setSelectedBed] = useState(null);
  const [showAdmit, setShowAdmit] = useState(false);
  const [admitBed, setAdmitBed]   = useState(null);
  const [admitForm, setAdmitForm] = useState({ name: '', uhid: '', doctor: 'Dr. Arjun Mehta', reason: '' });
  const [view, setView]           = useState('grid'); // grid | list

  const ward = wards.find(w => w.id === activeWard);
  const occupied = ward.beds.filter(b => b.status === 'occupied').length;
  const available = ward.beds.filter(b => b.status === 'available').length;
  const cleaning  = ward.beds.filter(b => b.status === 'cleaning').length;

  const handleAdmit = () => {
    setWards(ws => ws.map(w => w.id === activeWard ? {
      ...w,
      beds: w.beds.map(b => b.bed === admitBed ? {
        ...b, status: 'occupied', patient: admitForm.name, uhid: admitForm.uhid || 'CC-NEW',
        days: 0, doctor: admitForm.doctor, condition: 'Stable',
      } : b),
    } : w));
    setShowAdmit(false);
    setAdmitForm({ name: '', uhid: '', doctor: 'Dr. Arjun Mehta', reason: '' });
  };

  const handleDischarge = (bedId) => {
    setWards(ws => ws.map(w => w.id === activeWard ? {
      ...w, beds: w.beds.map(b => b.bed === bedId ? { bed: b.bed, patient: null, status: 'cleaning' } : b),
    } : w));
    setSelectedBed(null);
  };

  return (
    <div className="fade-in" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 20 }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ fontSize: 18, fontWeight: 800 }}>IPD & Bed Management</div>
          <div style={{ fontSize: 13, color: CC.muted }}>Inpatient Department · Live bed tracker</div>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <div style={{ display: 'flex', gap: 4, background: '#F1F5F9', borderRadius: 8, padding: 3 }}>
            {['grid', 'list'].map(v => (
              <button key={v} onClick={() => setView(v)} style={{ padding: '5px 12px', borderRadius: 6, border: 'none', background: view === v ? '#fff' : 'transparent', fontWeight: view === v ? 700 : 500, fontSize: 12, color: view === v ? CC.primary : CC.muted, cursor: 'pointer', boxShadow: view === v ? '0 1px 4px rgba(0,0,0,0.1)' : 'none' }}>
                {v === 'grid' ? '⊞ Grid' : '☰ List'}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Ward tabs */}
      <div style={{ display: 'flex', gap: 10 }}>
        {wards.map(w => {
          const occ = w.beds.filter(b => b.status === 'occupied').length;
          const pct = Math.round((occ / w.total) * 100);
          const isActive = activeWard === w.id;
          return (
            <button key={w.id} onClick={() => setActiveWard(w.id)} style={{
              flex: 1, padding: '14px 16px', borderRadius: 12, border: `2px solid ${isActive ? w.color : CC.border}`,
              background: isActive ? `${w.color}12` : '#fff', cursor: 'pointer', textAlign: 'left', transition: 'all 0.2s',
            }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 }}>
                <div style={{ fontWeight: 700, fontSize: 13, color: isActive ? w.color : CC.text }}>{w.name}</div>
                <span style={{ fontWeight: 800, fontSize: 13, color: w.color }}>{pct}%</span>
              </div>
              <div style={{ height: 4, background: '#F1F5F9', borderRadius: 4 }}>
                <div style={{ height: '100%', width: `${pct}%`, background: w.color, borderRadius: 4 }} />
              </div>
              <div style={{ fontSize: 11, color: CC.muted, marginTop: 5 }}>{occ}/{w.total} occupied · {w.floor}</div>
            </button>
          );
        })}
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 14 }}>
        <StatCard label="Total Beds"  value={ward.total}    icon="🛏️" color="#EFF6FF" />
        <StatCard label="Occupied"    value={occupied}      icon="👤" color="#FEF2F2" />
        <StatCard label="Available"   value={available}     icon="✅" color="#ECFDF5" />
        <StatCard label="Cleaning"    value={cleaning}      icon="🧹" color="#FFFBEB" />
      </div>

      {/* Main view */}
      {view === 'grid' ? (
        <Card title={`${ward.name} – Bed Map`} action={<span style={{ fontSize: 12, color: CC.muted }}>{ward.floor}</span>}>
          <div style={{ padding: 20 }}>
            {/* Legend */}
            <div style={{ display: 'flex', gap: 16, marginBottom: 18, flexWrap: 'wrap' }}>
              {[
                { label: 'Occupied', color: CC.primary, bg: CC.sky },
                { label: 'Available', color: CC.success, bg: '#ECFDF5' },
                { label: 'Cleaning', color: CC.warning, bg: '#FFFBEB' },
                { label: 'Critical', color: CC.error, bg: '#FEF2F2' },
              ].map(l => (
                <div key={l.label} style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12 }}>
                  <div style={{ width: 12, height: 12, borderRadius: 3, background: l.bg, border: `1.5px solid ${l.color}` }} />
                  <span style={{ color: CC.muted }}>{l.label}</span>
                </div>
              ))}
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(110px, 1fr))', gap: 10 }}>
              {ward.beds.map((bed, i) => {
                const cond = bed.condition && COND_COLOR[bed.condition];
                const isCritical = bed.condition === 'Critical';
                const borderColor = bed.status === 'occupied'
                  ? (isCritical ? CC.error : CC.primary)
                  : bed.status === 'cleaning' ? CC.warning : CC.success;
                const bgColor = bed.status === 'occupied'
                  ? (isCritical ? '#FEF2F2' : '#EFF6FF')
                  : bed.status === 'cleaning' ? '#FFFBEB' : '#F0FDF4';

                return (
                  <div key={bed.bed} onClick={() => bed.status === 'available' ? (setAdmitBed(bed.bed), setShowAdmit(true)) : setSelectedBed(bed)} style={{
                    padding: '10px 8px', borderRadius: 10, border: `2px solid ${borderColor}`,
                    background: bgColor, cursor: 'pointer', textAlign: 'center',
                    transition: 'all 0.15s', position: 'relative',
                  }}
                  onMouseEnter={e => e.currentTarget.style.transform = 'scale(1.04)'}
                  onMouseLeave={e => e.currentTarget.style.transform = 'scale(1)'}
                  >
                    {isCritical && (
                      <div style={{ position: 'absolute', top: -6, right: -6, width: 14, height: 14, borderRadius: '50%', background: CC.error, border: '2px solid #fff', animation: 'pulse 1.5s infinite' }} />
                    )}
                    <div style={{ fontSize: 18, marginBottom: 4 }}>
                      {bed.status === 'available' ? '🛏️' : bed.status === 'cleaning' ? '🧹' : '👤'}
                    </div>
                    <div style={{ fontWeight: 800, fontSize: 11, color: borderColor }}>{bed.bed}</div>
                    {bed.patient && (
                      <div style={{ fontSize: 9, color: CC.muted, marginTop: 2, lineHeight: 1.3 }}>
                        {bed.patient.split(' ')[0]}
                      </div>
                    )}
                    {bed.status === 'occupied' && bed.days !== undefined && (
                      <div style={{ fontSize: 9, color: CC.muted }}>Day {bed.days}</div>
                    )}
                    {bed.status === 'available' && (
                      <div style={{ fontSize: 9, color: CC.success, fontWeight: 600 }}>+ Admit</div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        </Card>
      ) : (
        <Card title="Admitted Patients">
          <Table
            cols={['Bed', 'Patient', 'UHID', 'Doctor', 'Days', 'Condition', 'Actions']}
            rows={ward.beds.filter(b => b.status === 'occupied').map(b => {
              const cond = COND_COLOR[b.condition] || { c: CC.muted, bg: '#F1F5F9' };
              return { cells: [
                <span style={{ fontWeight: 800, color: CC.primary }}>{b.bed}</span>,
                <div style={{ fontWeight: 600 }}>{b.patient}</div>,
                <span style={{ fontSize: 12, color: CC.muted }}>{b.uhid}</span>,
                b.doctor,
                <span style={{ fontWeight: 700 }}>Day {b.days}</span>,
                <Badge color={cond.c} bg={cond.bg}>{b.condition}</Badge>,
                <div style={{ display: 'flex', gap: 6 }}>
                  <Btn size="sm" variant="ghost" onClick={() => setSelectedBed(b)}>View</Btn>
                  <Btn size="sm" variant="danger" onClick={() => handleDischarge(b.bed)}>Discharge</Btn>
                </div>,
              ]};
            })}
          />
        </Card>
      )}

      {/* Bed Detail Modal */}
      <Modal open={!!selectedBed} onClose={() => setSelectedBed(null)} title="Patient Bed Details" width={480}>
        {selectedBed && selectedBed.patient && (
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginBottom: 20, padding: '16px', background: '#F8FAFC', borderRadius: 12 }}>
              <div style={{ width: 56, height: 56, borderRadius: 14, background: `linear-gradient(135deg,${CC.primary},${CC.light})`, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontWeight: 800, fontSize: 20 }}>
                {selectedBed.patient.split(' ').map(n => n[0]).join('')}
              </div>
              <div>
                <div style={{ fontSize: 17, fontWeight: 800 }}>{selectedBed.patient}</div>
                <div style={{ fontSize: 12, color: CC.muted }}>{selectedBed.uhid}</div>
                {selectedBed.condition && (
                  <Badge color={COND_COLOR[selectedBed.condition]?.c} bg={COND_COLOR[selectedBed.condition]?.bg}>{selectedBed.condition}</Badge>
                )}
              </div>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, marginBottom: 20 }}>
              {[
                ['Bed Number', selectedBed.bed],
                ['Attending Doctor', selectedBed.doctor],
                ['Days Admitted', `Day ${selectedBed.days}`],
                ['Ward', ward.name],
              ].map(([k, v]) => (
                <div key={k} style={{ padding: '12px 14px', background: '#F8FAFC', borderRadius: 10 }}>
                  <div style={{ fontSize: 11, color: CC.muted, marginBottom: 2 }}>{k}</div>
                  <div style={{ fontSize: 13, fontWeight: 700 }}>{v}</div>
                </div>
              ))}
            </div>
            <div style={{ display: 'flex', gap: 10 }}>
              <Btn variant="danger" onClick={() => handleDischarge(selectedBed.bed)}>Discharge Patient</Btn>
              <Btn variant="ghost" onClick={() => setSelectedBed(null)}>Close</Btn>
            </div>
          </div>
        )}
      </Modal>

      {/* Admit Modal */}
      <Modal open={showAdmit} onClose={() => setShowAdmit(false)} title={`Admit Patient – Bed ${admitBed}`} width={460}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          <FormField label="Patient Name" required>
            <Input value={admitForm.name} onChange={e => setAdmitForm(f => ({ ...f, name: e.target.value }))} placeholder="Full name" />
          </FormField>
          <FormField label="UHID (if registered)">
            <Input value={admitForm.uhid} onChange={e => setAdmitForm(f => ({ ...f, uhid: e.target.value }))} placeholder="CC-2024XXXX" />
          </FormField>
          <FormField label="Admitting Doctor" required>
            <Select value={admitForm.doctor} onChange={e => setAdmitForm(f => ({ ...f, doctor: e.target.value }))} options={['Dr. Arjun Mehta', 'Dr. Kavya Kapoor', 'Dr. Rajan Singh', 'Dr. Lata Iyer', 'Dr. Nikhil Roy']} />
          </FormField>
          <FormField label="Reason for Admission" required>
            <Input value={admitForm.reason} onChange={e => setAdmitForm(f => ({ ...f, reason: e.target.value }))} placeholder="Primary diagnosis / reason" />
          </FormField>
          <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
            <Btn variant="ghost" onClick={() => setShowAdmit(false)}>Cancel</Btn>
            <Btn onClick={handleAdmit}>Admit Patient</Btn>
          </div>
        </div>
      </Modal>
    </div>
  );
}

Object.assign(window, { IPDModule });
