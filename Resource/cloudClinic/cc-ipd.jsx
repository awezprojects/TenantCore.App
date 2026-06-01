// CloudClinic – IPD & Bed Management (Wards › Rooms › Beds)
const { useState } = React;

const COND_COLOR = {
  Stable:     { c: CC.success, bg: '#ECFDF5' },
  Improving:  { c: '#0891B2',  bg: '#ECFEFF' },
  Serious:    { c: CC.warning, bg: '#FFFBEB' },
  Critical:   { c: CC.error,   bg: '#FEF2F2' },
  'Post-op':  { c: '#7C3AED',  bg: '#F5F3FF' },
};

// Single bed tile (shared by room clusters)
function BedTile({ bed, onClick }) {
  const isCritical = bed.condition === 'Critical';
  const borderColor = bed.status === 'occupied'
    ? (isCritical ? CC.error : CC.primary)
    : bed.status === 'cleaning' ? CC.warning : CC.success;
  const bgColor = bed.status === 'occupied'
    ? (isCritical ? '#FEF2F2' : '#EFF6FF')
    : bed.status === 'cleaning' ? '#FFFBEB' : '#F0FDF4';
  return (
    <div onClick={onClick} style={{
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
        <div style={{ fontSize: 9, color: CC.muted, marginTop: 2, lineHeight: 1.3 }}>{bed.patient.split(' ')[0]}</div>
      )}
      {bed.status === 'occupied' && bed.days !== undefined && (
        <div style={{ fontSize: 9, color: CC.muted }}>Day {bed.days}</div>
      )}
      {bed.status === 'available' && (
        <div style={{ fontSize: 9, color: CC.success, fontWeight: 600 }}>+ Admit</div>
      )}
    </div>
  );
}

function IPDModule() {
  const [wards, setWards]           = useWards();
  const [activeWard, setActiveWard] = useState(wards[0]?.id);
  const [activeRoom, setActiveRoom] = useState('all'); // 'all' or room id
  const [selectedBed, setSelectedBed] = useState(null);
  const [showAdmit, setShowAdmit]   = useState(false);
  const [admitBed, setAdmitBed]     = useState(null);
  const [admitRoom, setAdmitRoom]   = useState(null);
  const [admitForm, setAdmitForm]   = useState({ name: '', uhid: '', doctor: 'Dr. Arjun Mehta', reason: '' });
  const [view, setView]             = useState('grid'); // grid | list

  const ward = wards.find(w => w.id === activeWard) || wards[0];
  const allBeds = ward.rooms.flatMap(r => r.beds);
  const occupied  = allBeds.filter(b => b.status === 'occupied').length;
  const available = allBeds.filter(b => b.status === 'available').length;
  const cleaning  = allBeds.filter(b => b.status === 'cleaning').length;

  const visibleRooms = activeRoom === 'all' ? ward.rooms : ward.rooms.filter(r => r.id === activeRoom);

  const switchWard = (id) => { setActiveWard(id); setActiveRoom('all'); };

  const openAdmit = (roomId, bedId) => { setAdmitRoom(roomId); setAdmitBed(bedId); setShowAdmit(true); };

  const handleAdmit = () => {
    setWards(ws => ws.map(w => w.id === activeWard ? {
      ...w,
      rooms: w.rooms.map(r => r.id === admitRoom ? {
        ...r,
        beds: r.beds.map(b => b.bed === admitBed ? {
          ...b, status: 'occupied', patient: admitForm.name, uhid: admitForm.uhid || 'CC-NEW',
          days: 0, doctor: admitForm.doctor, condition: 'Stable',
        } : b),
      } : r),
    } : w));
    setShowAdmit(false);
    setAdmitForm({ name: '', uhid: '', doctor: 'Dr. Arjun Mehta', reason: '' });
  };

  const handleDischarge = (bedId) => {
    setWards(ws => ws.map(w => w.id === activeWard ? {
      ...w, rooms: w.rooms.map(r => ({ ...r, beds: r.beds.map(b => b.bed === bedId ? { bed: b.bed, patient: null, status: 'cleaning' } : b) })),
    } : w));
    setSelectedBed(null);
  };

  return (
    <div className="fade-in" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 20 }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ fontSize: 18, fontWeight: 800 }}>IPD & Bed Management</div>
          <div style={{ fontSize: 13, color: CC.muted }}>Inpatient Department · Wards › Rooms › Beds</div>
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

      {/* Breadcrumb */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13 }}>
        <span style={{ color: CC.muted, fontWeight: 600 }}>Wards</span>
        <span style={{ color: CC.border }}>›</span>
        <span style={{ color: activeRoom === 'all' ? ward.color : CC.muted, fontWeight: 700, cursor: 'pointer' }} onClick={() => setActiveRoom('all')}>{ward.name}</span>
        {activeRoom !== 'all' && (
          <React.Fragment>
            <span style={{ color: CC.border }}>›</span>
            <span style={{ color: ward.color, fontWeight: 700 }}>{ward.rooms.find(r => r.id === activeRoom)?.name}</span>
          </React.Fragment>
        )}
      </div>

      {/* Level 1 — Ward tabs */}
      <div style={{ display: 'flex', gap: 10 }}>
        {wards.map(w => {
          const beds = w.rooms.flatMap(r => r.beds);
          const occ = beds.filter(b => b.status === 'occupied').length;
          const pct = w.rooms.length ? Math.round((occ / bedCount(w)) * 100) : 0;
          const isActive = activeWard === w.id;
          return (
            <button key={w.id} onClick={() => switchWard(w.id)} style={{
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
              <div style={{ fontSize: 11, color: CC.muted, marginTop: 5 }}>{w.rooms.length} rooms · {occ}/{bedCount(w)} beds · {w.floor}</div>
            </button>
          );
        })}
      </div>

      {/* Level 2 — Room selector chips */}
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
        <span style={{ fontSize: 12, fontWeight: 700, color: CC.muted, marginRight: 2 }}>Rooms:</span>
        <button onClick={() => setActiveRoom('all')} style={{
          padding: '7px 14px', borderRadius: 20, border: `1.5px solid ${activeRoom === 'all' ? ward.color : CC.border}`,
          background: activeRoom === 'all' ? ward.color : '#fff', color: activeRoom === 'all' ? '#fff' : CC.text,
          fontWeight: 700, fontSize: 12, cursor: 'pointer', transition: 'all 0.15s',
        }}>All Rooms</button>
        {ward.rooms.map(r => {
          const occ = r.beds.filter(b => b.status === 'occupied').length;
          const isActive = activeRoom === r.id;
          return (
            <button key={r.id} onClick={() => setActiveRoom(r.id)} style={{
              padding: '7px 14px', borderRadius: 20, border: `1.5px solid ${isActive ? ward.color : CC.border}`,
              background: isActive ? `${ward.color}15` : '#fff', color: isActive ? ward.color : CC.text,
              fontWeight: isActive ? 700 : 500, fontSize: 12, cursor: 'pointer', transition: 'all 0.15s',
              display: 'flex', alignItems: 'center', gap: 6,
            }}>
              {r.name}
              <span style={{ fontSize: 10, fontWeight: 700, color: CC.muted, background: isActive ? '#fff' : '#F1F5F9', padding: '1px 6px', borderRadius: 10 }}>{occ}/{r.beds.length}</span>
            </button>
          );
        })}
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 14 }}>
        <StatCard label="Rooms"      value={ward.rooms.length} icon="🚪" color="#F5F3FF" />
        <StatCard label="Total Beds" value={bedCount(ward)}    icon="🛏️" color="#EFF6FF" />
        <StatCard label="Occupied"   value={occupied}          icon="👤" color="#FEF2F2" />
        <StatCard label="Available"  value={available}         icon="✅" color="#ECFDF5" />
        <StatCard label="Cleaning"   value={cleaning}          icon="🧹" color="#FFFBEB" />
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

            {/* Room clusters → beds */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              {visibleRooms.map(room => {
                const occ = room.beds.filter(b => b.status === 'occupied').length;
                return (
                  <div key={room.id} style={{ border: `1px solid ${CC.border}`, borderRadius: 14, overflow: 'hidden' }}>
                    {/* Room header */}
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '10px 16px', background: '#F8FAFC', borderBottom: `1px solid ${CC.border}` }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                        <div style={{ width: 8, height: 8, borderRadius: '50%', background: ward.color }} />
                        <span style={{ fontWeight: 800, fontSize: 14 }}>{room.name}</span>
                        <Badge color={ward.color} bg={`${ward.color}15`}>{room.type}</Badge>
                      </div>
                      <span style={{ fontSize: 12, fontWeight: 700, color: CC.muted }}>{occ}/{room.beds.length} beds occupied</span>
                    </div>
                    {/* Beds inside room */}
                    <div style={{ padding: 14, display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(110px, 1fr))', gap: 10 }}>
                      {room.beds.map(bed => (
                        <BedTile key={bed.bed} bed={bed}
                          onClick={() => bed.status === 'available' ? openAdmit(room.id, bed.bed) : setSelectedBed({ ...bed, roomName: room.name })} />
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </Card>
      ) : (
        <Card title="Admitted Patients">
          <Table
            cols={['Bed', 'Room', 'Patient', 'UHID', 'Doctor', 'Days', 'Condition', 'Actions']}
            rows={ward.rooms.flatMap(r => r.beds.filter(b => b.status === 'occupied').map(b => ({ ...b, roomName: r.name }))).map(b => {
              const cond = COND_COLOR[b.condition] || { c: CC.muted, bg: '#F1F5F9' };
              return { cells: [
                <span style={{ fontWeight: 800, color: CC.primary }}>{b.bed}</span>,
                <span style={{ fontSize: 12, fontWeight: 600 }}>{b.roomName}</span>,
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
                ['Room', selectedBed.roomName || ward.rooms.find(r => r.beds.some(b => b.bed === selectedBed.bed))?.name],
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
      <Modal open={showAdmit} onClose={() => setShowAdmit(false)} title={`Admit Patient – ${ward.rooms.find(r => r.id === admitRoom)?.name || ''} · Bed ${admitBed}`} width={460}>
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
