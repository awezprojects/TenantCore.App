// CloudClinic – Appointments Page & Prescriptions List Page
const { useState } = React;

// ── Today's Appointments Full Page ─────────────────────────────────────────
function AppointmentsPage({ onNav }) {
  const [filter, setFilter] = useState('all');
  const [date] = useState(new Date().toISOString().slice(0, 10));

  const statusColor = {
    'completed':   { c: CC.success, bg: '#ECFDF5', label: 'Visited' },
    'in-progress': { c: CC.primary, bg: CC.sky,   label: 'In Progress' },
    'waiting':     { c: CC.warning, bg: '#FFFBEB',label: 'Waiting' },
  };

  const filtered = filter === 'all' ? TODAY_APPOINTMENTS : TODAY_APPOINTMENTS.filter(a => a.status === filter);
  const counts = {
    all: TODAY_APPOINTMENTS.length,
    completed: TODAY_APPOINTMENTS.filter(a => a.status === 'completed').length,
    'in-progress': TODAY_APPOINTMENTS.filter(a => a.status === 'in-progress').length,
    waiting: TODAY_APPOINTMENTS.filter(a => a.status === 'waiting').length,
  };

  return (
    <div className="fade-in" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 18 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <button onClick={() => onNav('dashboard')} style={{ background: 'none', border: 'none', color: CC.muted, cursor: 'pointer', fontSize: 14 }}>← Back</button>
            <div style={{ fontSize: 18, fontWeight: 800 }}>Today's Appointments</div>
          </div>
          <div style={{ fontSize: 13, color: CC.muted, marginLeft: 38 }}>{new Date(date).toLocaleDateString('en-IN', { dateStyle: 'long' })} · {TODAY_APPOINTMENTS.length} patients</div>
        </div>
        <Btn onClick={() => onNav('prescription')}>+ New Prescription</Btn>
      </div>

      {/* Filter tabs */}
      <Card style={{ padding: 14 }}>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {[
            ['all', `All (${counts.all})`],
            ['completed', `Visited (${counts.completed})`],
            ['in-progress', `In Progress (${counts['in-progress']})`],
            ['waiting', `Waiting (${counts.waiting})`],
          ].map(([id, label]) => (
            <button key={id} onClick={() => setFilter(id)} style={{
              padding: '8px 14px', borderRadius: 8, border: `1.5px solid ${filter === id ? CC.primary : CC.border}`,
              background: filter === id ? CC.sky : '#fff', color: filter === id ? CC.primary : CC.muted,
              fontWeight: filter === id ? 700 : 500, fontSize: 12, cursor: 'pointer',
            }}>{label}</button>
          ))}
        </div>
      </Card>

      <Card>
        <Table
          cols={['Token', 'Time', 'Patient', 'Reason', 'Status', 'Actions']}
          rows={filtered.map(a => {
            const sm = statusColor[a.status];
            return { cells: [
              <span style={{ fontWeight: 800, color: CC.primary, fontSize: 13 }}>{a.id}</span>,
              <span style={{ fontWeight: 700 }}>{a.time}</span>,
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <div style={{ width: 32, height: 32, borderRadius: 8, background: `linear-gradient(135deg,${CC.primary},${CC.light})`, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 11 }}>{a.name.split(' ').map(n => n[0]).join('')}</div>
                <div>
                  <div style={{ fontWeight: 600 }}>{a.name}</div>
                  <div style={{ fontSize: 11, color: CC.muted }}>{a.uhid} · {a.age}{a.gender}</div>
                </div>
              </div>,
              <span style={{ fontSize: 12, color: CC.muted }}>{a.reason}</span>,
              <Badge color={sm.c} bg={sm.bg}>{sm.label}</Badge>,
              <div style={{ display: 'flex', gap: 6 }}>
                {a.status === 'waiting' && <Btn size="sm" variant="light">Start</Btn>}
                {(a.status === 'in-progress' || a.status === 'waiting') && <Btn size="sm" onClick={() => onNav('prescription', { uhid: a.uhid })}>Prescribe</Btn>}
                {a.status === 'completed' && <Btn size="sm" variant="light" onClick={() => onNav('prescriptions-list', { uhid: a.uhid })}>View Rx</Btn>}
              </div>,
            ]};
          })}
        />
      </Card>
    </div>
  );
}

// ── Prescriptions List Page (with date filter, edit/print/delete) ──────────
function PrescriptionsListPage({ onNav, initialFilter }) {
  const today = '2026-05-12';
  const [prescriptions, setPrescriptions] = useState(PRESCRIPTIONS_DB);
  const [search, setSearch] = useState('');
  const [fromDate, setFromDate] = useState(today);
  const [toDate, setToDate] = useState(today);
  const [dateRange, setDateRange] = useState('today'); // today | week | month | custom
  const [delConfirm, setDelConfirm] = useState(null);
  const [viewing, setViewing] = useState(initialFilter?.rxId || null);

  const applyRange = (range) => {
    setDateRange(range);
    const t = new Date('2026-05-12');
    if (range === 'today') { setFromDate(today); setToDate(today); }
    else if (range === 'week')  { const d = new Date(t); d.setDate(d.getDate() - 7);  setFromDate(d.toISOString().slice(0,10)); setToDate(today); }
    else if (range === 'month') { const d = new Date(t); d.setMonth(d.getMonth() - 1); setFromDate(d.toISOString().slice(0,10)); setToDate(today); }
  };

  const filtered = prescriptions.filter(r => {
    const matchSearch = !search || r.name.toLowerCase().includes(search.toLowerCase()) || r.uhid.toLowerCase().includes(search.toLowerCase()) || r.id.toLowerCase().includes(search.toLowerCase());
    const matchUhid = !initialFilter?.uhid || r.uhid === initialFilter.uhid;
    const matchDate = r.date >= fromDate && r.date <= toDate;
    return matchSearch && matchDate && matchUhid;
  }).sort((a, b) => (b.date + b.time).localeCompare(a.date + a.time));

  const handleDelete = () => {
    setPrescriptions(ps => ps.filter(p => p.id !== delConfirm));
    setDelConfirm(null);
  };

  const viewingRx = prescriptions.find(p => p.id === viewing);

  if (viewing && viewingRx) {
    return <PrescriptionView rx={viewingRx} onBack={() => setViewing(null)} onEdit={() => onNav('prescription', { rxId: viewing })} onDelete={() => setDelConfirm(viewing)} onPrint={() => onNav('print-rx', { rxId: viewing })} />;
  }

  return (
    <div className="fade-in" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 18 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <button onClick={() => onNav('dashboard')} style={{ background: 'none', border: 'none', color: CC.muted, cursor: 'pointer', fontSize: 14 }}>← Back</button>
            <div style={{ fontSize: 18, fontWeight: 800 }}>Prescriptions</div>
          </div>
          <div style={{ fontSize: 13, color: CC.muted, marginLeft: 38 }}>{filtered.length} prescriptions found</div>
        </div>
        <Btn onClick={() => onNav('prescription')}>+ New Prescription</Btn>
      </div>

      {/* Filters */}
      <Card style={{ padding: 16 }}>
        <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center' }}>
          <div style={{ display: 'flex', gap: 4, background: '#F1F5F9', borderRadius: 8, padding: 3 }}>
            {[['today', 'Today'], ['week', 'Last 7d'], ['month', 'Last 30d'], ['custom', 'Custom']].map(([id, label]) => (
              <button key={id} onClick={() => applyRange(id)} style={{ padding: '6px 12px', borderRadius: 6, border: 'none', background: dateRange === id ? '#fff' : 'transparent', fontWeight: dateRange === id ? 700 : 500, fontSize: 12, color: dateRange === id ? CC.primary : CC.muted, cursor: 'pointer', boxShadow: dateRange === id ? '0 1px 3px rgba(0,0,0,0.1)' : 'none' }}>{label}</button>
            ))}
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <span style={{ fontSize: 12, color: CC.muted, fontWeight: 600 }}>From</span>
            <input type="date" value={fromDate} onChange={e => { setFromDate(e.target.value); setDateRange('custom'); }} style={{ padding: '7px 10px', border: `1.5px solid ${CC.border}`, borderRadius: 7, fontSize: 12 }} />
            <span style={{ fontSize: 12, color: CC.muted, fontWeight: 600 }}>To</span>
            <input type="date" value={toDate} onChange={e => { setToDate(e.target.value); setDateRange('custom'); }} style={{ padding: '7px 10px', border: `1.5px solid ${CC.border}`, borderRadius: 7, fontSize: 12 }} />
          </div>
          <div style={{ flex: 1, position: 'relative', minWidth: 200 }}>
            <span style={{ position: 'absolute', left: 11, top: '50%', transform: 'translateY(-50%)', color: CC.muted }}>🔍</span>
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by patient name, UHID, Rx ID…" style={{ width: '100%', padding: '7px 12px 7px 34px', border: `1.5px solid ${CC.border}`, borderRadius: 7, fontSize: 12, outline: 'none' }} />
          </div>
        </div>
      </Card>

      <Card>
        <Table
          cols={['Rx ID', 'Date', 'Patient', 'Diagnosis', 'Medicines', 'Follow-up', 'Actions']}
          rows={filtered.map(r => ({
            cells: [
              <span style={{ fontWeight: 700, color: CC.primary, fontSize: 12 }}>{r.id}</span>,
              <div>
                <div style={{ fontWeight: 600, fontSize: 12 }}>{r.date}</div>
                <div style={{ fontSize: 11, color: CC.muted }}>{r.time}</div>
              </div>,
              <div>
                <div style={{ fontWeight: 600, fontSize: 13 }}>{r.name}</div>
                <div style={{ fontSize: 11, color: CC.muted }}>{r.uhid} · {r.age}{r.gender}</div>
              </div>,
              <span style={{ fontSize: 12 }}>{r.diagnosis.length > 50 ? r.diagnosis.slice(0, 50) + '…' : r.diagnosis}</span>,
              <Badge color={CC.primary} bg={CC.sky}>{r.meds.length} drugs</Badge>,
              <span style={{ fontSize: 12, color: CC.muted }}>{r.followup || '—'}</span>,
              <div style={{ display: 'flex', gap: 6 }}>
                <Btn size="sm" variant="light" onClick={() => setViewing(r.id)}>View</Btn>
                <Btn size="sm" variant="ghost" onClick={() => onNav('prescription', { rxId: r.id })}>Edit</Btn>
                <Btn size="sm" variant="ghost" onClick={() => onNav('print-rx', { rxId: r.id })}>🖨️</Btn>
                <Btn size="sm" variant="ghost" onClick={() => setDelConfirm(r.id)} style={{ color: CC.error, borderColor: '#FCA5A5' }}>🗑️</Btn>
              </div>,
            ]
          }))}
        />
        {filtered.length === 0 && (
          <div style={{ padding: 40, textAlign: 'center', color: CC.muted }}>
            <div style={{ fontSize: 32, marginBottom: 8 }}>📋</div>
            No prescriptions found in this date range
          </div>
        )}
      </Card>

      {/* Delete Confirmation */}
      <Modal open={!!delConfirm} onClose={() => setDelConfirm(null)} title="Confirm Delete" width={420}>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', padding: '10px 0 4px' }}>
          <div style={{ width: 56, height: 56, borderRadius: '50%', background: '#FEF2F2', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 26, marginBottom: 14 }}>⚠️</div>
          <div style={{ fontSize: 16, fontWeight: 700, marginBottom: 6, textAlign: 'center' }}>Delete this prescription?</div>
          <div style={{ fontSize: 13, color: CC.muted, textAlign: 'center', marginBottom: 18, lineHeight: 1.5 }}>
            Rx ID <strong style={{ color: CC.primary }}>{delConfirm}</strong> will be permanently deleted. This action cannot be undone.
          </div>
          <div style={{ display: 'flex', gap: 10, width: '100%', justifyContent: 'flex-end' }}>
            <Btn variant="ghost" onClick={() => setDelConfirm(null)}>Cancel</Btn>
            <Btn variant="danger" onClick={handleDelete}>Yes, Delete</Btn>
          </div>
        </div>
      </Modal>
    </div>
  );
}

// ── Prescription View (read-only inline) ──────────────────────────────────
function PrescriptionView({ rx, onBack, onEdit, onDelete, onPrint }) {
  return (
    <div className="fade-in" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 18 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <button onClick={onBack} style={{ background: 'none', border: 'none', color: CC.muted, cursor: 'pointer', fontSize: 14, marginBottom: 6 }}>← Back to list</button>
          <div style={{ fontSize: 18, fontWeight: 800 }}>Prescription · {rx.id}</div>
          <div style={{ fontSize: 13, color: CC.muted }}>{rx.date} at {rx.time}</div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <Btn variant="ghost" onClick={onEdit}>✏️ Edit</Btn>
          <Btn variant="ghost" onClick={onPrint}>🖨️ Print</Btn>
          <Btn variant="danger" onClick={onDelete}>🗑️ Delete</Btn>
        </div>
      </div>

      <Card>
        <div style={{ padding: '18px 24px', background: `linear-gradient(135deg,${CC.navy},#1565C0)`, color: '#fff' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              <div style={{ fontSize: 18, fontWeight: 800 }}>{rx.name}</div>
              <div style={{ fontSize: 12, opacity: 0.7 }}>{rx.uhid} · {rx.age}{rx.gender}</div>
            </div>
            <div style={{ textAlign: 'right', fontSize: 12, opacity: 0.7 }}>
              <div>Dr. Arjun Mehta</div>
              <div>{rx.date} {rx.time}</div>
            </div>
          </div>
        </div>
        <div style={{ padding: 24 }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6,1fr)', gap: 8, marginBottom: 16 }}>
            {Object.entries(rx.vitals).map(([k, v]) => (
              <div key={k} style={{ padding: '8px 10px', background: '#F8FAFC', borderRadius: 8, textAlign: 'center' }}>
                <div style={{ fontSize: 10, color: CC.muted, textTransform: 'uppercase' }}>{k}</div>
                <div style={{ fontSize: 14, fontWeight: 700 }}>{v}</div>
              </div>
            ))}
          </div>

          <div style={{ marginBottom: 16 }}>
            <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 6, color: CC.muted, textTransform: 'uppercase' }}>Diagnosis</div>
            <div style={{ fontSize: 14, padding: '10px 12px', background: '#F8FAFC', borderRadius: 8 }}>{rx.diagnosis}</div>
          </div>

          <div style={{ marginBottom: 16 }}>
            <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 8, color: CC.muted, textTransform: 'uppercase' }}>℞ Medicines</div>
            {rx.meds.map((m, i) => (
              <div key={i} style={{ padding: '12px 14px', background: '#F8FAFC', borderRadius: 8, marginBottom: 6, display: 'flex', justifyContent: 'space-between' }}>
                <div>
                  <div style={{ fontWeight: 700, fontSize: 14 }}>{i+1}. {m.drug} {m.strength}</div>
                  <div style={{ fontSize: 12, color: CC.muted }}>{m.form} · {m.freq} · {typeof m.duration === 'number' ? `${m.duration} days` : m.duration} · {m.timing}</div>
                  {(() => {
                    const list = Array.isArray(m.instructions) ? m.instructions : (m.instructions ? [m.instructions] : []);
                    return list.length > 0 && <div style={{ fontSize: 12, color: CC.primary, marginTop: 2 }}>📌 {list.join(' · ')}</div>;
                  })()}
                </div>
              </div>
            ))}
          </div>

          {rx.investigations.length > 0 && (
            <div style={{ marginBottom: 16 }}>
              <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 8, color: CC.muted, textTransform: 'uppercase' }}>Investigations</div>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                {rx.investigations.map((inv, i) => (
                  <span key={i} style={{ padding: '5px 12px', background: '#F5F3FF', color: '#7C3AED', borderRadius: 14, fontSize: 12, fontWeight: 600 }}>🔬 {inv}</span>
                ))}
              </div>
            </div>
          )}

          {rx.notes && (
            <div style={{ marginBottom: 16 }}>
              <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 6, color: CC.muted, textTransform: 'uppercase' }}>Notes</div>
              <div style={{ fontSize: 13, padding: '10px 12px', background: '#F8FAFC', borderRadius: 8, lineHeight: 1.6 }}>{rx.notes}</div>
            </div>
          )}

          <div style={{ padding: '12px 16px', background: CC.sky, borderRadius: 10, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              <div style={{ fontSize: 11, color: CC.muted, fontWeight: 600 }}>NEXT VISIT</div>
              <div style={{ fontSize: 15, fontWeight: 700, color: CC.primary }}>{new Date(rx.followup).toLocaleDateString('en-IN', { dateStyle: 'long' })}</div>
            </div>
            <Btn variant="primary" onClick={onPrint}>🖨️ Print Prescription</Btn>
          </div>
        </div>
      </Card>
    </div>
  );
}

Object.assign(window, { AppointmentsPage, PrescriptionsListPage, PrescriptionView });
