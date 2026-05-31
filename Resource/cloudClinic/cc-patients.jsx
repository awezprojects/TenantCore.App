// CloudClinic – Patient Registration & Management
const { useState, useRef } = React;

const PATIENTS_DATA = [
  { uhid: 'CC-20240042', name: 'Ramesh Kumar',  age: 52, gender: 'Male',   phone: '98765 43210', blood: 'B+', reg: '12 Jan 2024', status: 'Active', doctor: 'Dr. Mehta',  type: 'OPD' },
  { uhid: 'CC-20240078', name: 'Sunita Verma',  age: 34, gender: 'Female', phone: '97654 32109', blood: 'A+', reg: '28 Jan 2024', status: 'Active', doctor: 'Dr. Mehta',  type: 'OPD' },
  { uhid: 'CC-20240101', name: 'Anil Sharma',   age: 61, gender: 'Male',   phone: '96543 21098', blood: 'O+', reg: '05 Feb 2024', status: 'IPD',   doctor: 'Dr. Kapoor', type: 'IPD' },
  { uhid: 'CC-20240115', name: 'Meena Patel',   age: 28, gender: 'Female', phone: '95432 10987', blood: 'AB+',reg: '19 Feb 2024', status: 'Active', doctor: 'Dr. Mehta',  type: 'OPD' },
  { uhid: 'CC-20240119', name: 'Ravi Singh',    age: 45, gender: 'Male',   phone: '94321 09876', blood: 'B-', reg: '24 Feb 2024', status: 'Active', doctor: 'Dr. Singh',  type: 'OPD' },
  { uhid: 'CC-20240122', name: 'Geeta Nair',    age: 38, gender: 'Female', phone: '93210 98765', blood: 'A-', reg: '27 Feb 2024', status: 'Active', doctor: 'Dr. Mehta',  type: 'OPD' },
  { uhid: 'CC-20240135', name: 'Vijay Gupta',   age: 55, gender: 'Male',   phone: '92109 87654', blood: 'O-', reg: '10 Mar 2024', status: 'IPD',   doctor: 'Dr. Mehta',  type: 'IPD' },
  { uhid: 'CC-20240148', name: 'Sita Devi',     age: 42, gender: 'Female', phone: '91098 76543', blood: 'B+', reg: '22 Mar 2024', status: 'IPD',   doctor: 'Dr. Mehta',  type: 'IPD' },
];

const nextUHID = () => `CC-${new Date().getFullYear()}${String(Math.floor(Math.random()*9000)+1000)}`;

function PatientRegistration() {
  const [view, setView] = useState('list'); // list | register | detail
  const [selected, setSelected] = useState(null);
  const [search, setSearch] = useState('');
  const [patients, setPatients] = useState(PATIENTS_DATA);
  const [form, setForm] = useState({
    uhid: nextUHID(), name: '', dob: '', gender: 'Male', phone: '', email: '',
    address: '', city: '', blood: 'A+', emergency_name: '', emergency_phone: '',
    allergies: '', history: '', photo: null,
  });
  const [step, setStep] = useState(1);
  const [saved, setSaved] = useState(false);
  const fileRef = useRef();

  const filtered = patients.filter(p =>
    p.name.toLowerCase().includes(search.toLowerCase()) ||
    p.uhid.toLowerCase().includes(search.toLowerCase()) ||
    p.phone.includes(search)
  );

  const setF = (k, v) => setForm(f => ({ ...f, [k]: v }));

  const handleSave = () => {
    const age = form.dob ? Math.floor((new Date() - new Date(form.dob)) / 31557600000) : 0;
    const np = {
      uhid: form.uhid, name: form.name, age, gender: form.gender, phone: form.phone,
      blood: form.blood, reg: new Date().toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }),
      status: 'Active', doctor: 'Unassigned', type: 'OPD',
    };
    setPatients(p => [np, ...p]);
    setSaved(true);
    setTimeout(() => { setSaved(false); setView('list'); setStep(1); setForm({ uhid: nextUHID(), name: '', dob: '', gender: 'Male', phone: '', email: '', address: '', city: '', blood: 'A+', emergency_name: '', emergency_phone: '', allergies: '', history: '', photo: null }); }, 1600);
  };

  const statusColor = { Active: { c: CC.success, bg: '#ECFDF5' }, IPD: { c: CC.primary, bg: CC.sky }, Discharged: { c: CC.muted, bg: '#F1F5F9' } };

  if (view === 'register') {
    return (
      <div className="fade-in" style={{ padding: 24 }}>
        {/* Header */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 24 }}>
          <button onClick={() => { setView('list'); setStep(1); }} style={{ background: 'none', border: 'none', color: CC.muted, cursor: 'pointer', fontSize: 14 }}>← Back</button>
          <div style={{ fontSize: 18, fontWeight: 800 }}>New Patient Registration</div>
          <div style={{ marginLeft: 'auto', padding: '4px 12px', background: CC.sky, borderRadius: 20, fontSize: 12, fontWeight: 700, color: CC.primary }}>UHID: {form.uhid}</div>
        </div>

        {/* Step indicator */}
        <div style={{ display: 'flex', gap: 0, marginBottom: 28 }}>
          {['Personal Info', 'Contact & Address', 'Medical History'].map((s, i) => (
            <div key={s} style={{ flex: 1, display: 'flex', alignItems: 'center' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer' }} onClick={() => step > i + 1 && setStep(i + 1)}>
                <div style={{ width: 28, height: 28, borderRadius: '50%', background: step > i + 1 ? CC.success : step === i + 1 ? CC.primary : CC.border, color: step >= i + 1 ? '#fff' : CC.muted, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 12, fontWeight: 700, transition: 'all 0.3s' }}>
                  {step > i + 1 ? '✓' : i + 1}
                </div>
                <span style={{ fontSize: 13, fontWeight: step === i + 1 ? 700 : 400, color: step === i + 1 ? CC.primary : CC.muted }}>{s}</span>
              </div>
              {i < 2 && <div style={{ flex: 1, height: 2, background: step > i + 1 ? CC.success : CC.border, margin: '0 12px', transition: 'background 0.3s' }} />}
            </div>
          ))}
        </div>

        {saved && (
          <div style={{ background: '#ECFDF5', border: `1px solid ${CC.success}`, borderRadius: 10, padding: '14px 20px', display: 'flex', alignItems: 'center', gap: 10, marginBottom: 20, color: CC.success, fontWeight: 600 }}>
            ✅ Patient registered successfully! Redirecting...
          </div>
        )}

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 280px', gap: 20 }}>
          {/* Form */}
          <Card style={{ padding: 24 }}>
            {step === 1 && (
              <div className="fade-in">
                <div style={{ fontWeight: 700, marginBottom: 20, fontSize: 15 }}>Personal Information</div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                  <div style={{ gridColumn: '1/-1' }}>
                    <FormField label="Full Name" required>
                      <Input value={form.name} onChange={e => setF('name', e.target.value)} placeholder="e.g. Ramesh Kumar" />
                    </FormField>
                  </div>
                  <FormField label="Date of Birth" required>
                    <Input type="date" value={form.dob} onChange={e => setF('dob', e.target.value)} />
                  </FormField>
                  <FormField label="Gender" required>
                    <Select value={form.gender} onChange={e => setF('gender', e.target.value)} options={['Male', 'Female', 'Other']} />
                  </FormField>
                  <FormField label="Blood Group" required>
                    <Select value={form.blood} onChange={e => setF('blood', e.target.value)} options={['A+','A-','B+','B-','AB+','AB-','O+','O-']} />
                  </FormField>
                  <FormField label="Phone Number" required>
                    <Input value={form.phone} onChange={e => setF('phone', e.target.value)} placeholder="10-digit mobile number" />
                  </FormField>
                </div>
              </div>
            )}
            {step === 2 && (
              <div className="fade-in">
                <div style={{ fontWeight: 700, marginBottom: 20, fontSize: 15 }}>Contact & Address</div>
                <FormField label="Email Address">
                  <Input type="email" value={form.email} onChange={e => setF('email', e.target.value)} placeholder="patient@email.com" />
                </FormField>
                <FormField label="Address" required>
                  <textarea value={form.address} onChange={e => setF('address', e.target.value)} rows={2} placeholder="Street, locality…" style={{ width: '100%', padding: '9px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, fontFamily: 'inherit', resize: 'vertical' }} />
                </FormField>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                  <FormField label="City">
                    <Input value={form.city} onChange={e => setF('city', e.target.value)} placeholder="Mumbai" />
                  </FormField>
                </div>
                <div style={{ marginTop: 8, padding: '14px 16px', background: '#F8FAFC', borderRadius: 10, border: `1px solid ${CC.border}` }}>
                  <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 12, color: CC.text }}>Emergency Contact</div>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                    <FormField label="Name">
                      <Input value={form.emergency_name} onChange={e => setF('emergency_name', e.target.value)} placeholder="Contact name" />
                    </FormField>
                    <FormField label="Phone">
                      <Input value={form.emergency_phone} onChange={e => setF('emergency_phone', e.target.value)} placeholder="Phone number" />
                    </FormField>
                  </div>
                </div>
              </div>
            )}
            {step === 3 && (
              <div className="fade-in">
                <div style={{ fontWeight: 700, marginBottom: 20, fontSize: 15 }}>Medical History</div>
                <FormField label="Known Allergies">
                  <textarea value={form.allergies} onChange={e => setF('allergies', e.target.value)} rows={2} placeholder="e.g. Penicillin, Sulfa drugs, Peanuts…" style={{ width: '100%', padding: '9px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, fontFamily: 'inherit', resize: 'vertical' }} />
                </FormField>
                <FormField label="Past Medical History">
                  <textarea value={form.history} onChange={e => setF('history', e.target.value)} rows={4} placeholder="Diabetes, Hypertension, previous surgeries…" style={{ width: '100%', padding: '9px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, fontFamily: 'inherit', resize: 'vertical' }} />
                </FormField>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 8, marginTop: 8 }}>
                  {['Diabetes', 'Hypertension', 'Heart Disease', 'Asthma', 'Thyroid', 'Cancer'].map(c => (
                    <label key={c} style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, cursor: 'pointer', padding: '8px 10px', border: `1px solid ${CC.border}`, borderRadius: 8 }}>
                      <input type="checkbox" style={{ accentColor: CC.primary }} />
                      {c}
                    </label>
                  ))}
                </div>
              </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 24 }}>
              {step > 1 ? <Btn variant="ghost" onClick={() => setStep(s => s - 1)}>← Previous</Btn> : <div />}
              {step < 3
                ? <Btn onClick={() => setStep(s => s + 1)}>Next →</Btn>
                : <Btn onClick={handleSave}>✅ Register Patient</Btn>
              }
            </div>
          </Card>

          {/* Photo & UHID card */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
            <Card style={{ padding: 20 }}>
              <div style={{ fontWeight: 700, marginBottom: 14, fontSize: 14 }}>Patient Photo</div>
              <div onClick={() => fileRef.current?.click()} style={{
                width: '100%', paddingTop: '100%', background: CC.sky, borderRadius: 12, border: `2px dashed ${CC.primary}`,
                cursor: 'pointer', position: 'relative', overflow: 'hidden',
              }}>
                <div style={{ position: 'absolute', inset: 0, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', color: CC.primary }}>
                  {form.photo ? <img src={form.photo} alt="Patient" style={{ width: '100%', height: '100%', objectFit: 'cover' }} /> : (
                    <><div style={{ fontSize: 32 }}>📷</div><div style={{ fontSize: 12, fontWeight: 600, marginTop: 8 }}>Click to upload</div></>
                  )}
                </div>
              </div>
              <input ref={fileRef} type="file" accept="image/*" style={{ display: 'none' }} onChange={e => { const f = e.target.files[0]; if (f) { const r = new FileReader(); r.onload = ev => setF('photo', ev.target.result); r.readAsDataURL(f); } }} />
            </Card>
            <Card style={{ padding: 20 }}>
              <div style={{ fontWeight: 700, marginBottom: 12, fontSize: 14 }}>Registration Info</div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: 13 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}><span style={{ color: CC.muted }}>UHID</span><span style={{ fontWeight: 700, color: CC.primary }}>{form.uhid}</span></div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}><span style={{ color: CC.muted }}>Date</span><span style={{ fontWeight: 600 }}>{new Date().toLocaleDateString('en-IN')}</span></div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}><span style={{ color: CC.muted }}>Type</span><span style={{ fontWeight: 600 }}>OPD</span></div>
              </div>
            </Card>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="fade-in" style={{ padding: 24 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <div style={{ fontSize: 18, fontWeight: 800 }}>Patient Registry</div>
          <div style={{ fontSize: 13, color: CC.muted }}>{patients.length} patients registered</div>
        </div>
        <Btn onClick={() => setView('register')}>+ Register New Patient</Btn>
      </div>

      {/* Search */}
      <Card style={{ marginBottom: 16, padding: 16 }}>
        <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
          <div style={{ flex: 1, position: 'relative' }}>
            <span style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)', color: CC.muted }}>🔍</span>
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by name, UHID, or phone…" style={{ width: '100%', padding: '9px 12px 9px 36px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, outline: 'none' }} />
          </div>
          <Select value="" onChange={() => {}} options={[{ value: '', label: 'All Status' }, 'Active', 'IPD', 'Discharged']} />
        </div>
      </Card>

      <Card>
        <Table
          cols={['UHID', 'Patient', 'Age / Gender', 'Blood', 'Phone', 'Type', 'Status', 'Action']}
          rows={filtered.map(p => ({
            cells: [
              <span style={{ fontWeight: 700, color: CC.primary, fontSize: 12 }}>{p.uhid}</span>,
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <div style={{ width: 32, height: 32, borderRadius: 8, background: `linear-gradient(135deg,${CC.primary},${CC.light})`, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 12 }}>{p.name.split(' ').map(n => n[0]).join('')}</div>
                <span style={{ fontWeight: 600 }}>{p.name}</span>
              </div>,
              `${p.age}y / ${p.gender}`,
              <span style={{ padding: '2px 8px', background: '#FEF2F2', color: CC.error, borderRadius: 6, fontSize: 12, fontWeight: 700 }}>{p.blood}</span>,
              p.phone,
              <Badge color={p.type === 'IPD' ? CC.primary : CC.success} bg={p.type === 'IPD' ? CC.sky : '#ECFDF5'}>{p.type}</Badge>,
              <Badge color={statusColor[p.status]?.c || CC.muted} bg={statusColor[p.status]?.bg || '#F1F5F9'}>{p.status}</Badge>,
              <Btn size="sm" variant="light" onClick={() => { setSelected(p); setView('detail'); }}>View</Btn>,
            ]
          }))}
        />
      </Card>
    </div>
  );
}

Object.assign(window, { PatientRegistration, PATIENTS_DATA });
