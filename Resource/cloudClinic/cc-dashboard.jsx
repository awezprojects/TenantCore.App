// CloudClinic – Doctor Dashboard (3 sections: Appointments / Today's Visits / Prescriptions)
const { useState } = React;

function DoctorDashboard({ onNav }) {
  const appointments = TODAY_APPOINTMENTS;
  const visited = appointments.filter(a => a.status === 'completed').slice(0, 5);
  const upcoming = appointments.filter(a => a.status !== 'completed');
  const todayRx = PRESCRIPTIONS_DB.filter(r => r.date === '2026-05-12').slice(0, 5);

  const statusColor = {
    'completed':   { c: CC.success, bg: '#ECFDF5', label: 'Visited' },
    'in-progress': { c: CC.primary, bg: CC.sky,   label: 'In Progress' },
    'waiting':     { c: CC.warning, bg: '#FFFBEB',label: 'Waiting' },
  };

  return (
    <div className="fade-in" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 20 }}>
      {/* Greeting */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ fontSize: 20, fontWeight: 800, color: CC.text }}>Good morning, Dr. Mehta 👋</div>
          <div style={{ fontSize: 13, color: CC.muted, marginTop: 2 }}>
            {appointments.length} appointments today · {visited.length} completed · {upcoming.length} pending
          </div>
        </div>
        <Btn onClick={() => onNav('prescription')}>+ New Prescription</Btn>
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 14 }}>
        <StatCard label="Today's Appointments" value={appointments.length} icon="📅" color="#EFF6FF" trend={12} />
        <StatCard label="Visited" value={visited.length} sub="Consultations done" icon="✅" color="#ECFDF5" />
        <StatCard label="Pending" value={upcoming.length} sub="In queue" icon="⏳" color="#FFFBEB" />
        <StatCard label="Rx Today" value={todayRx.length} sub="Prescriptions written" icon="📋" color="#FAF5FF" />
      </div>

      {/* TWO sections: Appointments + Today's Visited */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        {/* Appointments List */}
        <Card
          title="📅 Today's Appointments"
          action={<Btn size="sm" variant="light" onClick={() => onNav('appointments')}>View all →</Btn>}
        >
          <div>
            {appointments.slice(0, 5).map((a, i) => {
              const sm = statusColor[a.status];
              return (
                <div key={a.id} style={{
                  display: 'flex', alignItems: 'center', gap: 12, padding: '12px 18px',
                  borderBottom: i < 4 ? `1px solid ${CC.border}` : 'none',
                  background: a.status === 'in-progress' ? '#F0F7FF' : 'transparent',
                }}>
                  <div style={{ width: 44, textAlign: 'center', flexShrink: 0 }}>
                    <div style={{ fontSize: 13, fontWeight: 800, color: CC.text }}>{a.time}</div>
                  </div>
                  <div style={{ width: 32, height: 32, borderRadius: 8, background: `linear-gradient(135deg,${CC.primary},${CC.light})`, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontWeight: 700, fontSize: 11, flexShrink: 0 }}>
                    {a.name.split(' ').map(n => n[0]).join('')}
                  </div>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ fontSize: 13, fontWeight: 600, color: CC.text }}>{a.name} <span style={{ fontWeight: 400, color: CC.muted, fontSize: 11 }}>· {a.age}{a.gender}</span></div>
                    <div style={{ fontSize: 11, color: CC.muted, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{a.reason}</div>
                  </div>
                  <Badge color={sm.c} bg={sm.bg}>{sm.label}</Badge>
                </div>
              );
            })}
          </div>
        </Card>

        {/* Visited Today */}
        <Card
          title="✅ Today's Visited Patients"
          action={<Btn size="sm" variant="light" onClick={() => onNav('appointments')}>View all →</Btn>}
        >
          <div>
            {visited.map((a, i) => (
              <div key={a.id} style={{
                display: 'flex', alignItems: 'center', gap: 12, padding: '12px 18px',
                borderBottom: i < visited.length - 1 ? `1px solid ${CC.border}` : 'none',
              }}>
                <div style={{ width: 44, textAlign: 'center', flexShrink: 0 }}>
                  <div style={{ fontSize: 13, fontWeight: 800, color: CC.success }}>{a.time}</div>
                </div>
                <div style={{ width: 32, height: 32, borderRadius: 8, background: '#ECFDF5', display: 'flex', alignItems: 'center', justifyContent: 'center', color: CC.success, fontWeight: 700, fontSize: 11, flexShrink: 0 }}>
                  {a.name.split(' ').map(n => n[0]).join('')}
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontSize: 13, fontWeight: 600, color: CC.text }}>{a.name}</div>
                  <div style={{ fontSize: 11, color: CC.muted }}>{a.uhid} · {a.reason}</div>
                </div>
                <Badge color={CC.success} bg="#ECFDF5">✓ Done</Badge>
              </div>
            ))}
          </div>
        </Card>
      </div>

      {/* Today's Prescriptions */}
      <Card
        title="📋 Recent Prescriptions"
        action={<Btn size="sm" variant="light" onClick={() => onNav('prescriptions-list')}>View all →</Btn>}
      >
        <Table
          cols={['Rx ID', 'Patient', 'Time', 'Diagnosis', 'Meds', 'Follow-up', 'Action']}
          rows={todayRx.map(r => ({
            cells: [
              <span style={{ fontWeight: 700, color: CC.primary, fontSize: 12 }}>{r.id}</span>,
              <div>
                <div style={{ fontWeight: 600, fontSize: 13 }}>{r.name}</div>
                <div style={{ fontSize: 11, color: CC.muted }}>{r.uhid} · {r.age}{r.gender}</div>
              </div>,
              <span style={{ fontWeight: 600 }}>{r.time}</span>,
              <span style={{ fontSize: 12 }}>{r.diagnosis.length > 40 ? r.diagnosis.slice(0, 40) + '…' : r.diagnosis}</span>,
              <Badge color={CC.primary} bg={CC.sky}>{r.meds.length} drugs</Badge>,
              <span style={{ fontSize: 12, color: CC.muted }}>{r.followup}</span>,
              <Btn size="sm" variant="light" onClick={() => onNav('prescriptions-list', { rxId: r.id })}>View Rx</Btn>,
            ]
          }))}
        />
      </Card>
    </div>
  );
}

// ── Receptionist (kept simple) ─────────────────────────────────────────────
function ReceptionDashboard({ onNav }) {
  return (
    <div className="fade-in" style={{ padding: 24 }}>
      <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 6 }}>Good morning, Priya 👋</div>
      <div style={{ fontSize: 13, color: CC.muted, marginBottom: 20 }}>Front Desk · 5 patients in queue today</div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 14, marginBottom: 20 }}>
        <StatCard label="Walk-ins" value="5" icon="🚶" color="#EFF6FF" />
        <StatCard label="Admitted (IPD)" value="18" icon="🛏️" color="#F0FDF4" />
        <StatCard label="Discharged" value="3" icon="🏠" color="#FFFBEB" />
        <StatCard label="Bed Occ." value="72%" icon="📊" color="#FAF5FF" />
      </div>
      <div style={{ display: 'flex', gap: 12 }}>
        <Btn onClick={() => onNav('patients')}>+ Register Patient</Btn>
        <Btn variant="ghost" onClick={() => onNav('opd')}>OPD Queue</Btn>
        <Btn variant="ghost" onClick={() => onNav('ipd')}>IPD Beds</Btn>
      </div>
    </div>
  );
}

function AdminDashboard({ onNav }) {
  return (
    <div className="fade-in" style={{ padding: 24 }}>
      <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 20 }}>Clinic Overview 📊</div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 14, marginBottom: 20 }}>
        <StatCard label="Total Patients" value="1,284" icon="👥" color="#EFF6FF" trend={6} />
        <StatCard label="Revenue Today" value="₹48,200" icon="💰" color="#ECFDF5" trend={14} />
        <StatCard label="Active Doctors" value="4/5" icon="🩺" color="#FFFBEB" />
        <StatCard label="Beds Available" value="14/52" icon="🛏️" color="#FAF5FF" />
      </div>
      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
        <Btn onClick={() => onNav('patients')}>Patients</Btn>
        <Btn onClick={() => onNav('opd')}>OPD</Btn>
        <Btn onClick={() => onNav('ipd')}>IPD</Btn>
        <Btn onClick={() => onNav('medicine')}>Medicines</Btn>
      </div>
    </div>
  );
}

function PatientDashboard({ onNav }) {
  return (
    <div className="fade-in" style={{ padding: 24 }}>
      <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 20 }}>My Health</div>
    </div>
  );
}

Object.assign(window, { DoctorDashboard, ReceptionDashboard, AdminDashboard, PatientDashboard });
