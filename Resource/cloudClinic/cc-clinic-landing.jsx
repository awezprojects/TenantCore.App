// CloudClinic – Doctor's Multi-Clinic Landing Dashboard
const { useState } = React;

const LINKED_CLINICS = [
  {
    id: 'cc-main', name: 'CloudClinic Main Hospital', type: 'Hospital',
    address: '123 Healthcare Avenue, Bandra West, Mumbai 400050',
    phone: '+91 22 4567 8900', logo: '🏥', color: '#1565C0',
    role: 'Senior Consultant – Cardiology',
    permissions: ['OPD', 'IPD', 'Prescription', 'Patient Records', 'Medicines'],
    stats: { todayAppts: 10, pendingRx: 3, beds: '14/52' },
    joined: '12 Jan 2023', status: 'active', ownership: 'employed',
  },
  {
    id: 'heartcare', name: 'HeartCare Speciality Clinic', type: 'Speciality Clinic',
    address: '45 Linking Road, Khar West, Mumbai 400052',
    phone: '+91 22 2604 1122', logo: '❤️', color: '#DC2626',
    role: 'Visiting Cardiologist',
    permissions: ['OPD', 'Prescription', 'Patient Records'],
    stats: { todayAppts: 4, pendingRx: 1, beds: 'N/A' },
    joined: '08 Mar 2024', status: 'active', ownership: 'visiting',
  },
  {
    id: 'wellness', name: 'Wellness Polyclinic', type: 'Polyclinic',
    address: '7 Hill Road, Bandra West, Mumbai 400050',
    phone: '+91 22 2640 5577', logo: '🌿', color: '#059669',
    role: 'Consulting Physician',
    permissions: ['OPD', 'Prescription'],
    stats: { todayAppts: 2, pendingRx: 0, beds: 'N/A' },
    joined: '22 Aug 2024', status: 'active', ownership: 'visiting',
  },
];

function DoctorClinicLanding({ onEnter, onLogout }) {
  const [showRegister, setShowRegister] = useState(false);
  const [hasOwnClinic, setHasOwnClinic] = useState(() => localStorage.getItem('cc_own_clinic') === '1');
  const [ownClinicData, setOwnClinicData] = useState(() => {
    const raw = localStorage.getItem('cc_own_clinic_data');
    return raw ? JSON.parse(raw) : null;
  });
  const [regForm, setRegForm] = useState({
    name: '', type: 'Speciality Clinic', address: '', city: 'Mumbai', state: 'Maharashtra',
    pincode: '', phone: '', email: '', regNo: '', specialty: 'Cardiology', staff: '1-5',
  });
  const [regStep, setRegStep] = useState(1);
  const [regSuccess, setRegSuccess] = useState(false);

  const setRF = (k, v) => setRegForm(f => ({ ...f, [k]: v }));

  const allClinics = hasOwnClinic && ownClinicData
    ? [...LINKED_CLINICS, { ...ownClinicData, id: 'own-' + (ownClinicData.id || 'clinic'), logo: '👑', color: '#7C3AED', role: 'Owner & Founder', permissions: ['OPD','IPD','Prescription','Patient Records','Medicines','Admin'], stats: { todayAppts: 0, pendingRx: 0, beds: 'N/A' }, joined: 'Today', status: 'active', ownership: 'owned' }]
    : LINKED_CLINICS;

  const handleRegister = () => {
    const data = { ...regForm };
    setOwnClinicData(data);
    setHasOwnClinic(true);
    localStorage.setItem('cc_own_clinic', '1');
    localStorage.setItem('cc_own_clinic_data', JSON.stringify(data));
    setRegSuccess(true);
    setTimeout(() => { setShowRegister(false); setRegSuccess(false); setRegStep(1); }, 1800);
  };

  return (
    <div style={{ minHeight: '100vh', background: 'linear-gradient(180deg, #F0F4F9 0%, #E8EEF7 100%)' }}>
      {/* Top header bar */}
      <div style={{ background: '#fff', borderBottom: `1px solid ${CC.border}`, padding: '14px 36px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{ width: 40, height: 40, borderRadius: 10, background: `linear-gradient(135deg,${CC.primary},${CC.mid})`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 20 }}>🏥</div>
          <div>
            <div style={{ fontSize: 15, fontWeight: 800, letterSpacing: -0.3 }}>CloudClinic</div>
            <div style={{ fontSize: 10, color: CC.muted, letterSpacing: 0.5 }}>DOCTOR PORTAL</div>
          </div>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
          <div style={{ textAlign: 'right' }}>
            <div style={{ fontSize: 13, fontWeight: 700 }}>Dr. Arjun Mehta</div>
            <div style={{ fontSize: 11, color: CC.muted }}>MBBS, MD · MCI-12345</div>
          </div>
          <div style={{ width: 38, height: 38, borderRadius: 10, background: `linear-gradient(135deg,${CC.primary},${CC.light})`, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontWeight: 800, fontSize: 14 }}>AM</div>
          <button onClick={onLogout} style={{ background: 'none', border: 'none', cursor: 'pointer', color: CC.muted, fontSize: 18, padding: 8 }}>🚪</button>
        </div>
      </div>

      <div className="fade-in" style={{ maxWidth: 1280, margin: '0 auto', padding: '32px 36px' }}>
        {/* Greeting */}
        <div style={{ marginBottom: 28 }}>
          <div style={{ fontSize: 26, fontWeight: 800, color: CC.navy, letterSpacing: -0.5 }}>Welcome back, Dr. Mehta 👋</div>
          <div style={{ fontSize: 14, color: CC.muted, marginTop: 4 }}>
            You're linked with <strong style={{ color: CC.primary }}>{allClinics.length} clinic{allClinics.length !== 1 ? 's' : ''}</strong>. Select one to start your day.
          </div>
        </div>

        {/* Quick stats row */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 14, marginBottom: 28 }}>
          <div style={{ background: '#fff', borderRadius: 14, padding: '18px 20px', boxShadow: '0 2px 12px rgba(0,0,0,0.04)', border: `1px solid ${CC.border}` }}>
            <div style={{ fontSize: 11, fontWeight: 600, color: CC.muted, textTransform: 'uppercase', letterSpacing: 0.5 }}>Linked Clinics</div>
            <div style={{ fontSize: 28, fontWeight: 800, color: CC.text, marginTop: 4 }}>{allClinics.length}</div>
            <div style={{ fontSize: 11, color: CC.muted, marginTop: 2 }}>Active practice locations</div>
          </div>
          <div style={{ background: '#fff', borderRadius: 14, padding: '18px 20px', boxShadow: '0 2px 12px rgba(0,0,0,0.04)', border: `1px solid ${CC.border}` }}>
            <div style={{ fontSize: 11, fontWeight: 600, color: CC.muted, textTransform: 'uppercase', letterSpacing: 0.5 }}>Today's Appointments</div>
            <div style={{ fontSize: 28, fontWeight: 800, color: CC.primary, marginTop: 4 }}>{allClinics.reduce((s, c) => s + c.stats.todayAppts, 0)}</div>
            <div style={{ fontSize: 11, color: CC.muted, marginTop: 2 }}>Across all clinics</div>
          </div>
          <div style={{ background: '#fff', borderRadius: 14, padding: '18px 20px', boxShadow: '0 2px 12px rgba(0,0,0,0.04)', border: `1px solid ${CC.border}` }}>
            <div style={{ fontSize: 11, fontWeight: 600, color: CC.muted, textTransform: 'uppercase', letterSpacing: 0.5 }}>Pending Rx</div>
            <div style={{ fontSize: 28, fontWeight: 800, color: CC.warning, marginTop: 4 }}>{allClinics.reduce((s, c) => s + c.stats.pendingRx, 0)}</div>
            <div style={{ fontSize: 11, color: CC.muted, marginTop: 2 }}>Awaiting completion</div>
          </div>
          <div style={{ background: '#fff', borderRadius: 14, padding: '18px 20px', boxShadow: '0 2px 12px rgba(0,0,0,0.04)', border: `1px solid ${CC.border}` }}>
            <div style={{ fontSize: 11, fontWeight: 600, color: CC.muted, textTransform: 'uppercase', letterSpacing: 0.5 }}>Own Practice</div>
            <div style={{ fontSize: 28, fontWeight: 800, color: hasOwnClinic ? CC.success : CC.muted, marginTop: 4 }}>{hasOwnClinic ? '✓' : '—'}</div>
            <div style={{ fontSize: 11, color: CC.muted, marginTop: 2 }}>{hasOwnClinic ? 'Registered' : 'Not registered'}</div>
          </div>
        </div>

        {/* Section heading */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 14 }}>
          <div>
            <div style={{ fontSize: 17, fontWeight: 800, color: CC.text }}>My Clinics</div>
            <div style={{ fontSize: 12, color: CC.muted, marginTop: 2 }}>Click any card to enter that clinic</div>
          </div>
        </div>

        {/* Clinic cards grid */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(360px, 1fr))', gap: 16, marginBottom: 32 }}>
          {allClinics.map(c => (
            <div key={c.id} onClick={() => onEnter(c)} style={{
              background: '#fff', borderRadius: 16, overflow: 'hidden', cursor: 'pointer',
              boxShadow: '0 2px 14px rgba(0,0,0,0.05)', border: `1px solid ${CC.border}`,
              transition: 'all 0.2s ease',
            }}
            onMouseEnter={e => { e.currentTarget.style.transform = 'translateY(-3px)'; e.currentTarget.style.boxShadow = `0 8px 24px ${c.color}25`; e.currentTarget.style.borderColor = c.color; }}
            onMouseLeave={e => { e.currentTarget.style.transform = 'translateY(0)'; e.currentTarget.style.boxShadow = '0 2px 14px rgba(0,0,0,0.05)'; e.currentTarget.style.borderColor = CC.border; }}
            >
              {/* Banner */}
              <div style={{ height: 70, background: `linear-gradient(135deg, ${c.color}, ${c.color}cc)`, position: 'relative', padding: '14px 18px', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                  <div style={{ width: 44, height: 44, borderRadius: 12, background: 'rgba(255,255,255,0.25)', backdropFilter: 'blur(8px)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 22 }}>{c.logo}</div>
                  <div>
                    <div style={{ color: '#fff', fontWeight: 800, fontSize: 15, lineHeight: 1.2 }}>{c.name}</div>
                    <div style={{ color: 'rgba(255,255,255,0.85)', fontSize: 11, marginTop: 2 }}>{c.type}</div>
                  </div>
                </div>
                {c.ownership === 'owned' && (
                  <span style={{ padding: '3px 8px', background: 'rgba(255,255,255,0.25)', backdropFilter: 'blur(8px)', borderRadius: 12, fontSize: 10, fontWeight: 700, color: '#fff', letterSpacing: 0.5 }}>OWNER</span>
                )}
              </div>

              {/* Body */}
              <div style={{ padding: 18 }}>
                <div style={{ marginBottom: 14 }}>
                  <div style={{ fontSize: 11, color: CC.muted, fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.4 }}>My Role</div>
                  <div style={{ fontSize: 13, fontWeight: 700, color: CC.text, marginTop: 2 }}>{c.role}</div>
                </div>

                <div style={{ display: 'flex', alignItems: 'flex-start', gap: 6, marginBottom: 6, fontSize: 12, color: CC.muted }}>
                  <span style={{ marginTop: 1 }}>📍</span><span>{c.address}</span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 14, fontSize: 12, color: CC.muted }}>
                  <span>📞</span><span>{c.phone}</span>
                </div>

                {/* Quick stats */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 8, marginBottom: 14, padding: '10px 0', borderTop: `1px solid ${CC.border}`, borderBottom: `1px solid ${CC.border}` }}>
                  <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: 18, fontWeight: 800, color: c.color }}>{c.stats.todayAppts}</div>
                    <div style={{ fontSize: 10, color: CC.muted, fontWeight: 600 }}>TODAY'S APPT</div>
                  </div>
                  <div style={{ textAlign: 'center', borderLeft: `1px solid ${CC.border}`, borderRight: `1px solid ${CC.border}` }}>
                    <div style={{ fontSize: 18, fontWeight: 800, color: c.stats.pendingRx > 0 ? CC.warning : CC.muted }}>{c.stats.pendingRx}</div>
                    <div style={{ fontSize: 10, color: CC.muted, fontWeight: 600 }}>PENDING RX</div>
                  </div>
                  <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: 14, fontWeight: 800, color: CC.text, paddingTop: 4 }}>{c.stats.beds}</div>
                    <div style={{ fontSize: 10, color: CC.muted, fontWeight: 600 }}>BEDS</div>
                  </div>
                </div>

                {/* Permissions */}
                <div style={{ marginBottom: 14 }}>
                  <div style={{ fontSize: 10, color: CC.muted, fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 6 }}>Permissions</div>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 5 }}>
                    {c.permissions.map(p => (
                      <span key={p} style={{ padding: '3px 8px', background: `${c.color}12`, color: c.color, borderRadius: 12, fontSize: 10, fontWeight: 700 }}>{p}</span>
                    ))}
                  </div>
                </div>

                {/* Footer */}
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingTop: 8 }}>
                  <div style={{ fontSize: 10, color: CC.muted }}>Linked since {c.joined}</div>
                  <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, color: c.color, fontWeight: 700, fontSize: 13 }}>
                    Enter Clinic <span style={{ fontSize: 14 }}>→</span>
                  </span>
                </div>
              </div>
            </div>
          ))}

          {/* Register own clinic card */}
          {!hasOwnClinic && (
            <div onClick={() => setShowRegister(true)} style={{
              borderRadius: 16, border: `2px dashed ${CC.primary}`, background: 'rgba(21,101,192,0.03)',
              cursor: 'pointer', padding: 24, display: 'flex', flexDirection: 'column',
              alignItems: 'center', justifyContent: 'center', minHeight: 320,
              textAlign: 'center', transition: 'all 0.2s',
            }}
            onMouseEnter={e => { e.currentTarget.style.background = 'rgba(21,101,192,0.08)'; e.currentTarget.style.transform = 'translateY(-3px)'; }}
            onMouseLeave={e => { e.currentTarget.style.background = 'rgba(21,101,192,0.03)'; e.currentTarget.style.transform = 'translateY(0)'; }}
            >
              <div style={{ width: 64, height: 64, borderRadius: 18, background: `linear-gradient(135deg,${CC.primary},${CC.light})`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 30, marginBottom: 14, boxShadow: '0 8px 20px rgba(21,101,192,0.25)' }}>➕</div>
              <div style={{ fontSize: 17, fontWeight: 800, color: CC.text, marginBottom: 6 }}>Register Your Own Clinic</div>
              <div style={{ fontSize: 12, color: CC.muted, maxWidth: 280, lineHeight: 1.5, marginBottom: 14 }}>
                Start your own practice on CloudClinic. Complete control over staff, patients, and operations.
              </div>
              <span style={{ padding: '7px 18px', background: CC.primary, color: '#fff', borderRadius: 20, fontSize: 12, fontWeight: 700 }}>Register Now</span>
              <div style={{ fontSize: 10, color: CC.muted, marginTop: 12, fontStyle: 'italic' }}>* 1 application per doctor account</div>
            </div>
          )}
        </div>

        {/* Already have own clinic — info card */}
        {hasOwnClinic && (
          <div style={{ background: '#fff', borderRadius: 14, padding: '16px 20px', border: `1px solid ${CC.border}`, display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <div style={{ width: 38, height: 38, borderRadius: 10, background: '#ECFDF5', color: CC.success, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 18 }}>✅</div>
              <div>
                <div style={{ fontSize: 13, fontWeight: 700 }}>Your clinic is registered</div>
                <div style={{ fontSize: 11, color: CC.muted }}>Doctors can register only 1 application per account. Contact support to add more.</div>
              </div>
            </div>
            <span style={{ fontSize: 11, color: CC.muted, fontWeight: 600 }}>1 of 1 used</span>
          </div>
        )}
      </div>

      {/* Registration Modal */}
      <Modal open={showRegister} onClose={() => setShowRegister(false)} title="Register Your Clinic" width={620}>
        {regSuccess ? (
          <div style={{ textAlign: 'center', padding: '20px 0' }}>
            <div style={{ width: 72, height: 72, borderRadius: '50%', background: '#ECFDF5', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 36, margin: '0 auto 16px' }}>🎉</div>
            <div style={{ fontSize: 18, fontWeight: 800, marginBottom: 6 }}>Clinic Registered Successfully!</div>
            <div style={{ fontSize: 13, color: CC.muted, marginBottom: 16 }}>Your clinic has been added to your account. You can now enter it from the dashboard.</div>
            <div style={{ padding: '12px 16px', background: CC.sky, borderRadius: 10, fontSize: 12, color: CC.primary, fontWeight: 600 }}>
              Application ID: <strong>APP-{Date.now().toString().slice(-6)}</strong>
            </div>
          </div>
        ) : (
          <>
            {/* Step indicator */}
            <div style={{ display: 'flex', gap: 0, marginBottom: 22 }}>
              {['Basic Info', 'Contact & Address', 'Practice Details'].map((s, i) => (
                <div key={s} style={{ flex: 1, display: 'flex', alignItems: 'center' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <div style={{ width: 26, height: 26, borderRadius: '50%', background: regStep > i + 1 ? CC.success : regStep === i + 1 ? CC.primary : CC.border, color: regStep >= i + 1 ? '#fff' : CC.muted, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 11, fontWeight: 700 }}>
                      {regStep > i + 1 ? '✓' : i + 1}
                    </div>
                    <span style={{ fontSize: 12, fontWeight: regStep === i + 1 ? 700 : 500, color: regStep === i + 1 ? CC.primary : CC.muted }}>{s}</span>
                  </div>
                  {i < 2 && <div style={{ flex: 1, height: 2, background: regStep > i + 1 ? CC.success : CC.border, margin: '0 10px' }} />}
                </div>
              ))}
            </div>

            {regStep === 1 && (
              <div className="fade-in">
                <FormField label="Clinic / Hospital Name" required>
                  <Input value={regForm.name} onChange={e => setRF('name', e.target.value)} placeholder="e.g. Mehta Heart Clinic" />
                </FormField>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  <FormField label="Establishment Type" required>
                    <Select value={regForm.type} onChange={e => setRF('type', e.target.value)} options={['Speciality Clinic', 'Polyclinic', 'Hospital', 'Diagnostic Centre', 'Home Practice']} />
                  </FormField>
                  <FormField label="Primary Specialty" required>
                    <Select value={regForm.specialty} onChange={e => setRF('specialty', e.target.value)} options={['Cardiology', 'General Medicine', 'Orthopaedics', 'Gynaecology', 'Paediatrics', 'Dermatology', 'ENT', 'Other']} />
                  </FormField>
                </div>
                <FormField label="Medical Council Reg. No." required>
                  <Input value={regForm.regNo} onChange={e => setRF('regNo', e.target.value)} placeholder="MCI-XXXXX" />
                </FormField>
              </div>
            )}

            {regStep === 2 && (
              <div className="fade-in">
                <FormField label="Full Address" required>
                  <textarea value={regForm.address} onChange={e => setRF('address', e.target.value)} rows={2} placeholder="Building, street, locality…"
                    style={{ width: '100%', padding: '9px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, fontFamily: 'inherit', resize: 'vertical' }} />
                </FormField>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
                  <FormField label="City" required>
                    <Input value={regForm.city} onChange={e => setRF('city', e.target.value)} />
                  </FormField>
                  <FormField label="State" required>
                    <Input value={regForm.state} onChange={e => setRF('state', e.target.value)} />
                  </FormField>
                  <FormField label="Pincode" required>
                    <Input value={regForm.pincode} onChange={e => setRF('pincode', e.target.value)} placeholder="400050" />
                  </FormField>
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  <FormField label="Phone" required>
                    <Input value={regForm.phone} onChange={e => setRF('phone', e.target.value)} placeholder="+91 22 XXXX XXXX" />
                  </FormField>
                  <FormField label="Email">
                    <Input type="email" value={regForm.email} onChange={e => setRF('email', e.target.value)} placeholder="clinic@email.com" />
                  </FormField>
                </div>
              </div>
            )}

            {regStep === 3 && (
              <div className="fade-in">
                <FormField label="Staff Size">
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 8 }}>
                    {['1-5', '6-15', '16-50', '50+'].map(s => (
                      <button key={s} onClick={() => setRF('staff', s)} style={{
                        padding: '10px 8px', borderRadius: 10, border: `1.5px solid ${regForm.staff === s ? CC.primary : CC.border}`,
                        background: regForm.staff === s ? CC.sky : '#fff', color: regForm.staff === s ? CC.primary : CC.text,
                        fontWeight: 700, fontSize: 12, cursor: 'pointer',
                      }}>{s} members</button>
                    ))}
                  </div>
                </FormField>
                <div style={{ padding: '14px 16px', background: '#FFFBEB', border: '1px solid #FDE68A', borderRadius: 10, fontSize: 12, color: '#92400E', marginTop: 10, lineHeight: 1.6 }}>
                  <strong>📋 Note:</strong> Doctors can register <strong>only 1 clinic application</strong> per account. Once registered, contact support to modify or transfer ownership.
                </div>
                <div style={{ marginTop: 14, padding: 14, background: '#F8FAFC', borderRadius: 10 }}>
                  <div style={{ fontSize: 11, fontWeight: 700, color: CC.muted, textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 8 }}>Review</div>
                  <div style={{ fontSize: 13 }}><strong>{regForm.name || '(unnamed clinic)'}</strong></div>
                  <div style={{ fontSize: 12, color: CC.muted }}>{regForm.type} · {regForm.specialty}</div>
                  <div style={{ fontSize: 12, color: CC.muted }}>{regForm.address}, {regForm.city} {regForm.pincode}</div>
                </div>
              </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 22, paddingTop: 16, borderTop: `1px solid ${CC.border}` }}>
              {regStep > 1
                ? <Btn variant="ghost" onClick={() => setRegStep(s => s - 1)}>← Previous</Btn>
                : <Btn variant="ghost" onClick={() => setShowRegister(false)}>Cancel</Btn>}
              {regStep < 3
                ? <Btn onClick={() => setRegStep(s => s + 1)} disabled={regStep === 1 && (!regForm.name || !regForm.regNo)}>Next →</Btn>
                : <Btn onClick={handleRegister}>✅ Register Clinic</Btn>}
            </div>
          </>
        )}
      </Modal>
    </div>
  );
}

Object.assign(window, { DoctorClinicLanding, LINKED_CLINICS });
