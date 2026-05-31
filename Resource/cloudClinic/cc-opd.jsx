// CloudClinic – OPD Module
const { useState } = React;

const OPD_DATA = [
  { token:'T-001', uhid:'CC-20240042', name:'Ramesh Kumar',  age:52, gender:'Male',   doctor:'Dr. Arjun Mehta',  time:'09:00', dept:'Cardiology',   status:'completed',   reason:'Chest pain follow-up',     fee:500 },
  { token:'T-002', uhid:'CC-20240078', name:'Sunita Verma',  age:34, gender:'Female', doctor:'Dr. Arjun Mehta',  time:'09:30', dept:'Cardiology',   status:'completed',   reason:'Hypertension review',       fee:500 },
  { token:'T-003', uhid:'CC-20240101', name:'Anil Sharma',   age:61, gender:'Male',   doctor:'Dr. Arjun Mehta',  time:'10:00', dept:'Cardiology',   status:'in-progress', reason:'Shortness of breath',       fee:500 },
  { token:'T-004', uhid:'CC-20240115', name:'Meena Patel',   age:28, gender:'Female', doctor:'Dr. Arjun Mehta',  time:'10:30', dept:'Cardiology',   status:'waiting',     reason:'Annual checkup',             fee:500 },
  { token:'T-005', uhid:'CC-20240119', name:'Ravi Singh',    age:45, gender:'Male',   doctor:'Dr. Kavya Kapoor', time:'10:30', dept:'General Med.', status:'waiting',     reason:'ECG review',                 fee:400 },
  { token:'T-006', uhid:'CC-20240122', name:'Geeta Nair',    age:38, gender:'Female', doctor:'Dr. Arjun Mehta',  time:'11:00', dept:'Cardiology',   status:'waiting',     reason:'Palpitations',               fee:500 },
  { token:'T-007', uhid:'CC-20240135', name:'Vijay Gupta',   age:55, gender:'Male',   doctor:'Dr. Rajan Singh',  time:'11:30', dept:'Orthopaedics', status:'waiting',     reason:'Knee pain assessment',      fee:600 },
  { token:'T-008', uhid:'CC-20240148', name:'Priya Joshi',   age:29, gender:'Female', doctor:'Dr. Kavya Kapoor', time:'11:30', dept:'General Med.', status:'waiting',     reason:'Fever & cold 3 days',       fee:400 },
];

const STATUS_META = {
  completed:    { label: 'Completed',   c: CC.success, bg: '#ECFDF5' },
  'in-progress':{ label: 'In Progress', c: CC.primary, bg: CC.sky    },
  waiting:      { label: 'Waiting',     c: CC.warning, bg: '#FFFBEB' },
  cancelled:    { label: 'Cancelled',   c: CC.error,   bg: '#FEF2F2' },
};

function OPDModule({ onNav, role }) {
  const [records, setRecords]     = useState(OPD_DATA);
  const [filter, setFilter]       = useState('all');
  const [search, setSearch]       = useState('');
  const [selected, setSelected]   = useState(null);
  const [showReg, setShowReg]     = useState(false);
  const [newReg, setNewReg]       = useState({ uhid: '', name: '', doctor: 'Dr. Arjun Mehta', dept: 'Cardiology', reason: '', fee: '500' });

  const filtered = records.filter(r => {
    const matchFilter = filter === 'all' || r.status === filter;
    const matchSearch = r.name.toLowerCase().includes(search.toLowerCase()) ||
                        r.uhid.toLowerCase().includes(search.toLowerCase()) ||
                        r.token.toLowerCase().includes(search.toLowerCase());
    return matchFilter && matchSearch;
  });

  const counts = {
    all: records.length,
    waiting: records.filter(r => r.status === 'waiting').length,
    'in-progress': records.filter(r => r.status === 'in-progress').length,
    completed: records.filter(r => r.status === 'completed').length,
  };

  const setStatus = (token, status) => setRecords(rs => rs.map(r => r.token === token ? { ...r, status } : r));

  const handleRegister = () => {
    const token = `T-00${records.length + 1}`;
    setRecords(rs => [...rs, {
      token, uhid: newReg.uhid || 'CC-NEW', name: newReg.name, age: 30, gender: 'Unknown',
      doctor: newReg.doctor, time: new Date().toLocaleTimeString('en-IN',{hour:'2-digit',minute:'2-digit'}),
      dept: newReg.dept, status: 'waiting', reason: newReg.reason, fee: parseInt(newReg.fee) || 500,
    }]);
    setShowReg(false);
    setNewReg({ uhid: '', name: '', doctor: 'Dr. Arjun Mehta', dept: 'Cardiology', reason: '', fee: '500' });
  };

  return (
    <div className="fade-in" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 20 }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ fontSize: 18, fontWeight: 800 }}>OPD Management</div>
          <div style={{ fontSize: 13, color: CC.muted }}>Outpatient Department · {new Date().toLocaleDateString('en-IN', { dateStyle: 'long' })}</div>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          {role !== 'doctor' && <Btn onClick={() => setShowReg(true)}>+ New OPD Entry</Btn>}
        </div>
      </div>

      {/* Stats strip */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 14 }}>
        <StatCard label="Total Today"  value={counts.all}           icon="📋" color="#EFF6FF" />
        <StatCard label="Waiting"      value={counts.waiting}       icon="⏳" color="#FFFBEB" />
        <StatCard label="In Progress"  value={counts['in-progress']}icon="🩺" color={CC.sky} />
        <StatCard label="Completed"    value={counts.completed}     icon="✅" color="#ECFDF5" />
      </div>

      {/* Filters & search */}
      <Card style={{ padding: 16 }}>
        <div style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap' }}>
          <div style={{ display: 'flex', gap: 6 }}>
            {['all', 'waiting', 'in-progress', 'completed'].map(f => (
              <button key={f} onClick={() => setFilter(f)} style={{
                padding: '7px 14px', borderRadius: 8, border: `1.5px solid ${filter === f ? CC.primary : CC.border}`,
                background: filter === f ? CC.sky : '#fff', color: filter === f ? CC.primary : CC.muted,
                fontWeight: filter === f ? 700 : 500, fontSize: 12, cursor: 'pointer', transition: 'all 0.15s', textTransform: 'capitalize',
              }}>{f === 'all' ? `All (${counts.all})` : `${STATUS_META[f]?.label} (${counts[f]})`}</button>
            ))}
          </div>
          <div style={{ flex: 1, position: 'relative', minWidth: 200 }}>
            <span style={{ position: 'absolute', left: 11, top: '50%', transform: 'translateY(-50%)', color: CC.muted, fontSize: 14 }}>🔍</span>
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by name, UHID, token…" style={{ width: '100%', padding: '8px 12px 8px 34px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, outline: 'none' }} />
          </div>
        </div>
      </Card>

      {/* Table */}
      <Card>
        <Table
          cols={['Token', 'Patient', 'Doctor / Dept', 'Time', 'Reason', 'Fee', 'Status', 'Actions']}
          rows={filtered.map(r => {
            const sm = STATUS_META[r.status];
            return { cells: [
              <span style={{ fontWeight: 800, color: CC.primary, fontSize: 13 }}>{r.token}</span>,
              <div>
                <div style={{ fontWeight: 600, fontSize: 13 }}>{r.name}</div>
                <div style={{ fontSize: 11, color: CC.muted }}>{r.uhid} · {r.age}y {r.gender[0]}</div>
              </div>,
              <div>
                <div style={{ fontWeight: 600, fontSize: 12 }}>{r.doctor}</div>
                <div style={{ fontSize: 11, color: CC.muted }}>{r.dept}</div>
              </div>,
              <span style={{ fontWeight: 600, fontSize: 13 }}>{r.time}</span>,
              <span style={{ fontSize: 12, color: CC.muted }}>{r.reason}</span>,
              <span style={{ fontWeight: 700, color: CC.success }}>₹{r.fee}</span>,
              <Badge color={sm.c} bg={sm.bg}>{sm.label}</Badge>,
              <div style={{ display: 'flex', gap: 6 }}>
                {r.status === 'waiting' && (
                  <Btn size="sm" variant="light" onClick={() => setStatus(r.token, 'in-progress')}>Start</Btn>
                )}
                {r.status === 'in-progress' && role === 'doctor' && (
                  <Btn size="sm" onClick={() => onNav('prescription')}>Rx</Btn>
                )}
                {r.status === 'in-progress' && (
                  <Btn size="sm" variant="success" onClick={() => setStatus(r.token, 'completed')}>Done</Btn>
                )}
                <Btn size="sm" variant="ghost" onClick={() => setSelected(r)}>•••</Btn>
              </div>,
            ]};
          })}
        />
      </Card>

      {/* Detail Modal */}
      <Modal open={!!selected} onClose={() => setSelected(null)} title="OPD Visit Details" width={520}>
        {selected && (
          <div>
            <div style={{ display: 'flex', gap: 16, marginBottom: 20 }}>
              <div style={{ width: 60, height: 60, borderRadius: 14, background: `linear-gradient(135deg,${CC.primary},${CC.light})`, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontWeight: 800, fontSize: 20 }}>
                {selected.name.split(' ').map(n => n[0]).join('')}
              </div>
              <div>
                <div style={{ fontSize: 18, fontWeight: 800 }}>{selected.name}</div>
                <div style={{ fontSize: 13, color: CC.muted }}>{selected.uhid} · {selected.age}y · {selected.gender}</div>
                <Badge color={STATUS_META[selected.status].c} bg={STATUS_META[selected.status].bg}>{STATUS_META[selected.status].label}</Badge>
              </div>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 16 }}>
              {[
                ['Token', selected.token],
                ['Doctor', selected.doctor],
                ['Department', selected.dept],
                ['Appointment', selected.time],
                ['Reason', selected.reason],
                ['Fee', `₹${selected.fee}`],
              ].map(([k, v]) => (
                <div key={k} style={{ padding: '10px 14px', background: '#F8FAFC', borderRadius: 10 }}>
                  <div style={{ fontSize: 11, color: CC.muted, marginBottom: 2 }}>{k}</div>
                  <div style={{ fontSize: 13, fontWeight: 600 }}>{v}</div>
                </div>
              ))}
            </div>
            <div style={{ display: 'flex', gap: 10 }}>
              {selected.status === 'waiting' && <Btn onClick={() => { setStatus(selected.token, 'in-progress'); setSelected(null); }}>Mark In Progress</Btn>}
              {selected.status === 'in-progress' && <Btn onClick={() => onNav('prescription')}>Write Prescription</Btn>}
              <Btn variant="ghost" onClick={() => setSelected(null)}>Close</Btn>
            </div>
          </div>
        )}
      </Modal>

      {/* New OPD Registration Modal */}
      <Modal open={showReg} onClose={() => setShowReg(false)} title="New OPD Registration" width={480}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          <FormField label="Patient UHID / Name" required>
            <Input value={newReg.name} onChange={e => setNewReg(r => ({ ...r, name: e.target.value }))} placeholder="Search existing patient or enter name" />
          </FormField>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <FormField label="Assign Doctor" required>
              <Select value={newReg.doctor} onChange={e => setNewReg(r => ({ ...r, doctor: e.target.value }))} options={['Dr. Arjun Mehta', 'Dr. Kavya Kapoor', 'Dr. Rajan Singh', 'Dr. Lata Iyer', 'Dr. Nikhil Roy']} />
            </FormField>
            <FormField label="Department">
              <Select value={newReg.dept} onChange={e => setNewReg(r => ({ ...r, dept: e.target.value }))} options={['Cardiology', 'General Med.', 'Orthopaedics', 'Gynaecology', 'Paediatrics']} />
            </FormField>
          </div>
          <FormField label="Chief Complaint / Reason" required>
            <Input value={newReg.reason} onChange={e => setNewReg(r => ({ ...r, reason: e.target.value }))} placeholder="e.g. Fever, chest pain, routine checkup…" />
          </FormField>
          <FormField label="Consultation Fee (₹)">
            <Input value={newReg.fee} onChange={e => setNewReg(r => ({ ...r, fee: e.target.value }))} type="number" placeholder="500" />
          </FormField>
          <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
            <Btn variant="ghost" onClick={() => setShowReg(false)}>Cancel</Btn>
            <Btn onClick={handleRegister}>Register OPD</Btn>
          </div>
        </div>
      </Modal>
    </div>
  );
}

Object.assign(window, { OPDModule, OPD_DATA, STATUS_META });
