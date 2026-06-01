// CloudClinic – Admin: Wards, Rooms & Beds management
const { useState: useStateAW } = React;

// next sequential bed number for a ward (based on its prefix)
const nextBedStart = (ward) => {
  const nums = wardBeds(ward).map(b => parseInt((String(b.bed).match(/(\d+)\s*$/) || [])[1] || 0, 10));
  return (nums.length ? Math.max(...nums) : 0) + 1;
};
const padNum = (n) => String(n).padStart(2, '0');

// Status dot color for a bed chip
const bedDot = (b) => b.status === 'occupied' ? CC.primary : b.status === 'cleaning' ? CC.warning : CC.success;

function ColorSwatches({ value, onChange }) {
  return (
    <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
      {WARD_COLORS.map(c => (
        <button key={c} type="button" onClick={() => onChange(c)} style={{
          width: 30, height: 30, borderRadius: 8, background: c, cursor: 'pointer',
          border: value === c ? '3px solid #fff' : '3px solid transparent',
          boxShadow: value === c ? `0 0 0 2px ${c}` : 'inset 0 0 0 1px rgba(0,0,0,0.08)',
          transition: 'all 0.12s',
        }} />
      ))}
    </div>
  );
}

function AdminWardsModule() {
  const [wards, setWards] = useWards();
  const [expanded, setExpanded] = useStateAW(wards[0]?.id || null);
  // modal: { type:'ward'|'room'|'bed', mode:'create'|'edit', wardId, roomId, data }
  const [modal, setModal] = useStateAW(null);
  const [confirm, setConfirm] = useStateAW(null); // { label, onYes }

  const totalRooms = wards.reduce((a, w) => a + w.rooms.length, 0);
  const totalBeds  = wards.reduce((a, w) => a + bedCount(w), 0);
  const totalOcc   = wards.reduce((a, w) => a + occCount(w), 0);

  // ── Mutations ─────────────────────────────────────────────
  const saveWard = (data) => {
    if (modal.mode === 'create') {
      const id = slug(data.name) + '-' + Math.random().toString(36).slice(2, 5);
      setWards(ws => [...ws, { id, name: data.name, floor: data.floor, prefix: data.prefix || data.name[0].toUpperCase(), color: data.color, rooms: [] }]);
      setExpanded(id);
    } else {
      setWards(ws => ws.map(w => w.id === modal.wardId ? { ...w, name: data.name, floor: data.floor, prefix: data.prefix, color: data.color } : w));
    }
    setModal(null);
  };

  const saveRoom = (data) => {
    if (modal.mode === 'create') {
      const rid = slug(data.name) + '-' + Math.random().toString(36).slice(2, 4);
      setWards(ws => ws.map(w => w.id === modal.wardId ? { ...w, rooms: [...w.rooms, { id: rid, name: data.name, type: data.type, beds: [] }] } : w));
    } else {
      setWards(ws => ws.map(w => w.id === modal.wardId ? { ...w, rooms: w.rooms.map(r => r.id === modal.roomId ? { ...r, name: data.name, type: data.type } : r) } : w));
    }
    setModal(null);
  };

  const addBeds = (count) => {
    setWards(ws => ws.map(w => {
      if (w.id !== modal.wardId) return w;
      let n = nextBedStart(w);
      return { ...w, rooms: w.rooms.map(r => {
        if (r.id !== modal.roomId) return r;
        const fresh = Array.from({ length: count }, () => ({ bed: `${w.prefix}-${padNum(n++)}`, patient: null, status: 'available' }));
        return { ...r, beds: [...r.beds, ...fresh] };
      })};
    }));
    setModal(null);
  };

  const deleteBed = (wardId, roomId, bedId) => setWards(ws => ws.map(w => w.id === wardId ? { ...w, rooms: w.rooms.map(r => r.id === roomId ? { ...r, beds: r.beds.filter(b => b.bed !== bedId) } : r) } : w));
  const deleteRoom = (wardId, roomId) => setWards(ws => ws.map(w => w.id === wardId ? { ...w, rooms: w.rooms.filter(r => r.id !== roomId) } : w));
  const deleteWard = (wardId) => setWards(ws => ws.filter(w => w.id !== wardId));

  return (
    <div className="fade-in" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 20 }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end' }}>
        <div>
          <div style={{ fontSize: 18, fontWeight: 800 }}>Wards, Rooms & Beds</div>
          <div style={{ fontSize: 13, color: CC.muted }}>Facility setup · Build your bed hierarchy</div>
        </div>
        <Btn onClick={() => setModal({ type: 'ward', mode: 'create', data: { name: '', floor: '', prefix: '', color: WARD_COLORS[0] } })}>＋ New Ward</Btn>
      </div>

      {/* Summary */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 14 }}>
        <StatCard label="Wards"     value={wards.length} icon="🏥" color="#EFF6FF" />
        <StatCard label="Rooms"     value={totalRooms}   icon="🚪" color="#F5F3FF" />
        <StatCard label="Beds"      value={totalBeds}    icon="🛏️" color="#ECFDF5" />
        <StatCard label="Occupied"  value={`${totalOcc}/${totalBeds}`} icon="👤" color="#FEF2F2" />
      </div>

      {/* Ward accordion */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
        {wards.map(w => {
          const isOpen = expanded === w.id;
          const occ = occCount(w);
          const beds = bedCount(w);
          const pct = beds ? Math.round((occ / beds) * 100) : 0;
          return (
            <Card key={w.id} style={{ overflow: 'visible' }}>
              {/* Ward header */}
              <div style={{ display: 'flex', alignItems: 'center', gap: 14, padding: '16px 20px', cursor: 'pointer', borderBottom: isOpen ? `1px solid ${CC.border}` : 'none' }}
                onClick={() => setExpanded(isOpen ? null : w.id)}>
                <div style={{ width: 44, height: 44, borderRadius: 12, background: `${w.color}18`, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                  <div style={{ width: 14, height: 14, borderRadius: 4, background: w.color }} />
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <span style={{ fontWeight: 800, fontSize: 15 }}>{w.name}</span>
                    <Badge color={w.color} bg={`${w.color}15`}>{w.floor}</Badge>
                    <span style={{ fontSize: 11, color: CC.muted, fontFamily: 'monospace', background: '#F1F5F9', padding: '2px 7px', borderRadius: 5 }}>{w.prefix}-</span>
                  </div>
                  <div style={{ fontSize: 12, color: CC.muted, marginTop: 3 }}>{w.rooms.length} rooms · {beds} beds · {occ} occupied ({pct}%)</div>
                </div>
                <div style={{ display: 'flex', gap: 8, alignItems: 'center' }} onClick={e => e.stopPropagation()}>
                  <Btn size="sm" variant="light" onClick={() => setModal({ type: 'room', mode: 'create', wardId: w.id, data: { name: '', type: ROOM_TYPES[0] } })}>＋ Room</Btn>
                  <Btn size="sm" variant="ghost" onClick={() => setModal({ type: 'ward', mode: 'edit', wardId: w.id, data: { name: w.name, floor: w.floor, prefix: w.prefix, color: w.color } })}>Edit</Btn>
                  <IconBtn title="Delete ward" onClick={() => setConfirm({ label: `Delete “${w.name}” and all its ${w.rooms.length} rooms / ${beds} beds?`, onYes: () => deleteWard(w.id) })}>🗑</IconBtn>
                  <span style={{ fontSize: 14, color: CC.muted, transform: isOpen ? 'rotate(90deg)' : 'none', transition: 'transform 0.2s', marginLeft: 2 }}>▸</span>
                </div>
              </div>

              {/* Rooms */}
              {isOpen && (
                <div style={{ padding: w.rooms.length ? '8px 20px 18px' : '0' }}>
                  {w.rooms.length === 0 ? (
                    <div style={{ padding: '28px 20px', textAlign: 'center', color: CC.muted }}>
                      <div style={{ fontSize: 13, marginBottom: 10 }}>No rooms yet in this ward.</div>
                      <Btn size="sm" variant="ghost" onClick={() => setModal({ type: 'room', mode: 'create', wardId: w.id, data: { name: '', type: ROOM_TYPES[0] } })}>＋ Add first room</Btn>
                    </div>
                  ) : w.rooms.map(r => {
                    const rOcc = r.beds.filter(b => b.status === 'occupied').length;
                    return (
                      <div key={r.id} style={{ border: `1px solid ${CC.border}`, borderRadius: 12, padding: '14px 16px', marginTop: 12 }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
                          <span style={{ fontWeight: 700, fontSize: 14 }}>{r.name}</span>
                          <Badge color={w.color} bg={`${w.color}12`}>{r.type}</Badge>
                          <span style={{ fontSize: 11, color: CC.muted }}>{rOcc}/{r.beds.length} occupied</span>
                          <div style={{ flex: 1 }} />
                          <Btn size="sm" variant="light" onClick={() => setModal({ type: 'bed', mode: 'create', wardId: w.id, roomId: r.id })}>＋ Bed</Btn>
                          <Btn size="sm" variant="ghost" onClick={() => setModal({ type: 'room', mode: 'edit', wardId: w.id, roomId: r.id, data: { name: r.name, type: r.type } })}>Edit</Btn>
                          <IconBtn title="Delete room" onClick={() => setConfirm({ label: `Delete room “${r.name}” and its ${r.beds.length} beds?`, onYes: () => deleteRoom(w.id, r.id) })}>🗑</IconBtn>
                        </div>
                        {/* Bed chips */}
                        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginTop: 12 }}>
                          {r.beds.length === 0 && <span style={{ fontSize: 12, color: CC.muted, fontStyle: 'italic' }}>No beds — add one →</span>}
                          {r.beds.map(b => (
                            <div key={b.bed} title={b.patient ? `${b.patient} · ${b.status}` : b.status} style={{
                              display: 'flex', alignItems: 'center', gap: 7, padding: '6px 8px 6px 10px',
                              border: `1px solid ${CC.border}`, borderRadius: 8, background: '#fff', fontSize: 12,
                            }}>
                              <span style={{ width: 8, height: 8, borderRadius: '50%', background: bedDot(b), flexShrink: 0 }} />
                              <span style={{ fontWeight: 700 }}>{b.bed}</span>
                              <button onClick={() => b.status === 'occupied'
                                  ? setConfirm({ label: `Bed ${b.bed} is occupied by ${b.patient}. Remove anyway?`, onYes: () => deleteBed(w.id, r.id, b.bed) })
                                  : deleteBed(w.id, r.id, b.bed)}
                                title="Remove bed" style={{ border: 'none', background: 'transparent', cursor: 'pointer', color: CC.muted, fontSize: 14, lineHeight: 1, padding: 0 }}
                                onMouseEnter={e => e.currentTarget.style.color = CC.error}
                                onMouseLeave={e => e.currentTarget.style.color = CC.muted}>×</button>
                            </div>
                          ))}
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </Card>
          );
        })}

        {wards.length === 0 && (
          <div style={{ padding: 48, textAlign: 'center', color: CC.muted, background: '#fff', borderRadius: 14 }}>
            <div style={{ fontSize: 32, marginBottom: 8 }}>🏥</div>
            <div style={{ fontWeight: 700, marginBottom: 4, color: CC.text }}>No wards configured</div>
            <div style={{ fontSize: 13, marginBottom: 16 }}>Start by creating your first ward.</div>
            <Btn onClick={() => setModal({ type: 'ward', mode: 'create', data: { name: '', floor: '', prefix: '', color: WARD_COLORS[0] } })}>＋ New Ward</Btn>
          </div>
        )}
      </div>

      {/* ── Modals ── */}
      {modal?.type === 'ward'  && <WardModal modal={modal} onClose={() => setModal(null)} onSave={saveWard} />}
      {modal?.type === 'room'  && <RoomModal modal={modal} ward={wards.find(w => w.id === modal.wardId)} onClose={() => setModal(null)} onSave={saveRoom} />}
      {modal?.type === 'bed'   && <BedModal  modal={modal} ward={wards.find(w => w.id === modal.wardId)} room={wards.find(w => w.id === modal.wardId)?.rooms.find(r => r.id === modal.roomId)} onClose={() => setModal(null)} onAdd={addBeds} />}

      {/* Confirm */}
      <Modal open={!!confirm} onClose={() => setConfirm(null)} title="Please confirm" width={420}>
        {confirm && (
          <div>
            <div style={{ fontSize: 14, color: CC.text, lineHeight: 1.5, marginBottom: 22 }}>{confirm.label}</div>
            <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
              <Btn variant="ghost" onClick={() => setConfirm(null)}>Cancel</Btn>
              <Btn variant="danger" onClick={() => { confirm.onYes(); setConfirm(null); }}>Delete</Btn>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}

// Small icon button
function IconBtn({ children, onClick, title }) {
  return (
    <button onClick={onClick} title={title} style={{
      width: 32, height: 32, borderRadius: 8, border: `1.5px solid ${CC.border}`, background: '#fff',
      cursor: 'pointer', fontSize: 14, display: 'flex', alignItems: 'center', justifyContent: 'center', transition: 'all 0.15s',
    }}
      onMouseEnter={e => { e.currentTarget.style.borderColor = CC.error; e.currentTarget.style.background = '#FEF2F2'; }}
      onMouseLeave={e => { e.currentTarget.style.borderColor = CC.border; e.currentTarget.style.background = '#fff'; }}>
      {children}
    </button>
  );
}

// ── Ward create/edit modal ──
function WardModal({ modal, onClose, onSave }) {
  const [f, setF] = useStateAW(modal.data);
  const valid = f.name.trim() && f.floor.trim();
  return (
    <Modal open onClose={onClose} title={modal.mode === 'create' ? 'New Ward' : 'Edit Ward'} width={460}>
      <FormField label="Ward Name" required>
        <Input value={f.name} onChange={e => setF({ ...f, name: e.target.value })} placeholder="e.g. Orthopedics Ward" />
      </FormField>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
        <FormField label="Floor" required>
          <Input value={f.floor} onChange={e => setF({ ...f, floor: e.target.value })} placeholder="e.g. 5th Floor" />
        </FormField>
        <FormField label="Bed Prefix">
          <Input value={f.prefix} onChange={e => setF({ ...f, prefix: e.target.value.toUpperCase() })} placeholder="e.g. ORT" />
        </FormField>
      </div>
      <FormField label="Ward Colour">
        <ColorSwatches value={f.color} onChange={c => setF({ ...f, color: c })} />
      </FormField>
      <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end', marginTop: 8 }}>
        <Btn variant="ghost" onClick={onClose}>Cancel</Btn>
        <Btn onClick={() => valid && onSave(f)} style={{ opacity: valid ? 1 : 0.5, pointerEvents: valid ? 'auto' : 'none' }}>{modal.mode === 'create' ? 'Create Ward' : 'Save Changes'}</Btn>
      </div>
    </Modal>
  );
}

// ── Room create/edit modal ──
function RoomModal({ modal, ward, onClose, onSave }) {
  const [f, setF] = useStateAW(modal.data);
  const valid = f.name.trim();
  return (
    <Modal open onClose={onClose} title={modal.mode === 'create' ? `New Room · ${ward?.name || ''}` : 'Edit Room'} width={440}>
      <FormField label="Room Name / Number" required>
        <Input value={f.name} onChange={e => setF({ ...f, name: e.target.value })} placeholder="e.g. Room 401" />
      </FormField>
      <FormField label="Room Type" required>
        <Select value={f.type} onChange={e => setF({ ...f, type: e.target.value })} options={ROOM_TYPES} />
      </FormField>
      <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end', marginTop: 8 }}>
        <Btn variant="ghost" onClick={onClose}>Cancel</Btn>
        <Btn onClick={() => valid && onSave(f)} style={{ opacity: valid ? 1 : 0.5, pointerEvents: valid ? 'auto' : 'none' }}>{modal.mode === 'create' ? 'Create Room' : 'Save Changes'}</Btn>
      </div>
    </Modal>
  );
}

// ── Bed add modal (single or bulk) ──
function BedModal({ modal, ward, room, onClose, onAdd }) {
  const [count, setCount] = useStateAW(1);
  const start = ward ? nextBedStart(ward) : 1;
  const preview = Array.from({ length: Math.min(count, 8) }, (_, i) => `${ward?.prefix}-${padNum(start + i)}`);
  return (
    <Modal open onClose={onClose} title={`Add Beds · ${room?.name || ''}`} width={440}>
      <FormField label="How many beds?">
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <button onClick={() => setCount(c => Math.max(1, c - 1))} style={stepBtn}>−</button>
          <div style={{ fontSize: 22, fontWeight: 800, minWidth: 40, textAlign: 'center' }}>{count}</div>
          <button onClick={() => setCount(c => Math.min(20, c + 1))} style={stepBtn}>＋</button>
          <span style={{ fontSize: 12, color: CC.muted, marginLeft: 4 }}>auto-numbered from the ward prefix</span>
        </div>
      </FormField>
      <div style={{ background: '#F8FAFC', borderRadius: 10, padding: '12px 14px', marginBottom: 18 }}>
        <div style={{ fontSize: 11, color: CC.muted, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 8 }}>Will create</div>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
          {preview.map(p => (
            <span key={p} style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '5px 9px', background: '#fff', border: `1px solid ${CC.border}`, borderRadius: 7, fontSize: 12, fontWeight: 700 }}>
              <span style={{ width: 7, height: 7, borderRadius: '50%', background: CC.success }} />{p}
            </span>
          ))}
          {count > 8 && <span style={{ fontSize: 12, color: CC.muted, alignSelf: 'center' }}>+{count - 8} more…</span>}
        </div>
      </div>
      <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
        <Btn variant="ghost" onClick={onClose}>Cancel</Btn>
        <Btn onClick={() => onAdd(count)}>Add {count} Bed{count > 1 ? 's' : ''}</Btn>
      </div>
    </Modal>
  );
}

const stepBtn = {
  width: 38, height: 38, borderRadius: 10, border: `1.5px solid ${CC.border}`, background: '#fff',
  cursor: 'pointer', fontSize: 18, fontWeight: 700, color: CC.text, display: 'flex', alignItems: 'center', justifyContent: 'center',
};

Object.assign(window, { AdminWardsModule });
