// CloudClinic – Prescription Writer (enhanced: language, history, charts, investigations)
const { useState, useMemo } = React;

const DRUG_DB = [
  { name: 'Amlodipine',     form: 'Tablet',    strengths: ['2.5mg','5mg','10mg'] },
  { name: 'Atorvastatin',   form: 'Tablet',    strengths: ['10mg','20mg','40mg','80mg'] },
  { name: 'Metoprolol',     form: 'Tablet',    strengths: ['25mg','50mg','100mg'] },
  { name: 'Ramipril',       form: 'Capsule',   strengths: ['2.5mg','5mg','10mg'] },
  { name: 'Aspirin',        form: 'Tablet',    strengths: ['75mg','150mg','325mg'] },
  { name: 'Paracetamol',    form: 'Tablet',    strengths: ['325mg','500mg','650mg'] },
  { name: 'Metformin',      form: 'Tablet',    strengths: ['500mg','850mg','1000mg'] },
  { name: 'Omeprazole',     form: 'Capsule',   strengths: ['10mg','20mg','40mg'] },
  { name: 'Amoxicillin',    form: 'Capsule',   strengths: ['250mg','500mg'] },
  { name: 'Azithromycin',   form: 'Tablet',    strengths: ['250mg','500mg'] },
  { name: 'Pantoprazole',   form: 'Tablet',    strengths: ['20mg','40mg'] },
  { name: 'Clopidogrel',    form: 'Tablet',    strengths: ['75mg'] },
  { name: 'Losartan',       form: 'Tablet',    strengths: ['25mg','50mg','100mg'] },
  { name: 'Furosemide',     form: 'Tablet',    strengths: ['20mg','40mg','80mg'] },
  { name: 'Insulin Glargine', form: 'Injection', strengths: ['100U/mL'] },
  { name: 'Cough Syrup',    form: 'Syrup',     strengths: ['100ml'] },
  { name: 'Cetirizine',     form: 'Syrup',     strengths: ['5mg/5ml'] },
  { name: 'Tobramycin',     form: 'Eye Drop',  strengths: ['0.3%'] },
  { name: 'Ofloxacin',      form: 'Ear Drop',  strengths: ['0.3%'] },
  { name: 'Mometasone',     form: 'Nasal Drop',strengths: ['50mcg'] },
  { name: 'Clobetasol',     form: 'Cream',     strengths: ['0.05%'] },
  { name: 'Betamethasone',  form: 'Ointment',  strengths: ['0.1%'] },
  { name: 'Salbutamol',     form: 'Inhaler',   strengths: ['100mcg'] },
];

const INTERACTIONS = {
  'Aspirin+Clopidogrel': '⚠️ Dual antiplatelet — increased bleeding risk. Monitor closely.',
  'Metformin+Furosemide': '⚠️ Furosemide may impair metformin excretion. Monitor renal function.',
  'Amlodipine+Metoprolol': 'ℹ️ Additive antihypertensive effect — monitor BP.',
};

function checkInteractions(meds) {
  const names = meds.map(m => m.drug);
  const found = [];
  Object.entries(INTERACTIONS).forEach(([pair, msg]) => {
    const [a, b] = pair.split('+');
    if (names.includes(a) && names.includes(b)) found.push(msg);
  });
  return found;
}

function DrugSearch({ onSelect }) {
  const [q, setQ] = useState('');
  const [open, setOpen] = useState(false);
  const results = q.length > 1 ? DRUG_DB.filter(d => d.name.toLowerCase().includes(q.toLowerCase())) : [];
  return (
    <div style={{ position: 'relative' }}>
      <input value={q} onChange={e => { setQ(e.target.value); setOpen(true); }}
        onFocus={() => setOpen(true)} onBlur={() => setTimeout(() => setOpen(false), 150)}
        placeholder="Search drug name (tablet, syrup, drops, cream, inhaler…)"
        style={{ width: '100%', padding: '9px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, outline: 'none' }} />
      {open && results.length > 0 && (
        <div style={{ position: 'absolute', top: '100%', left: 0, right: 0, background: '#fff', border: `1px solid ${CC.border}`, borderRadius: 10, boxShadow: '0 8px 24px rgba(0,0,0,0.12)', zIndex: 100, maxHeight: 260, overflowY: 'auto', marginTop: 4 }}>
          {results.map(d => (
            <div key={d.name} onMouseDown={() => { onSelect(d); setQ(''); setOpen(false); }}
              style={{ padding: '10px 14px', cursor: 'pointer', borderBottom: `1px solid ${CC.border}`, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
              onMouseEnter={e => e.currentTarget.style.background = CC.sky}
              onMouseLeave={e => e.currentTarget.style.background = '#fff'}>
              <div>
                <div style={{ fontWeight: 600, fontSize: 13 }}>{d.name}</div>
                <div style={{ fontSize: 11, color: CC.muted }}>{d.form} · {d.strengths.join(', ')}</div>
              </div>
              <span style={{ fontSize: 11, color: CC.primary, fontWeight: 600 }}>+ Add</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function PrescriptionModule({ ctx, onNav }) {
  // Load patient context: from rxId (edit), from uhid (new for this patient), default
  const initialRx = ctx?.rxId ? PRESCRIPTIONS_DB.find(p => p.id === ctx.rxId) : null;
  const initialUhid = initialRx?.uhid || ctx?.uhid || 'CC-20240042';
  const history = PATIENT_HISTORY[initialUhid] || PATIENT_HISTORY['CC-20240042'];

  const patient = {
    name: history.name, uhid: initialUhid, age: history.age, gender: history.gender, blood: history.blood,
    allergies: history.allergies, chronic: history.chronic,
    doctor: 'Dr. Arjun Mehta',
    date: new Date().toLocaleDateString('en-IN', { dateStyle: 'long' }),
  };

  // Settings (persist language)
  const [lang, setLang] = useState(() => localStorage.getItem('cc_rx_lang') || 'en');
  const setLanguage = (l) => { setLang(l); localStorage.setItem('cc_rx_lang', l); };

  // Form state
  const [vitals, setVitals] = useState(initialRx?.vitals || { bp: '', pulse: '', temp: '', weight: '', spo2: '', rr: '', sugar: '' });
  const [diagnosis, setDiagnosis] = useState(initialRx?.diagnosis || '');
  const [notes, setNotes] = useState(initialRx?.notes || '');
  const [followup, setFollowup] = useState(initialRx?.followup || '');
  // Normalise loaded meds: parse duration to integer days, ensure instructions is array
  const normaliseMed = (m, i) => {
    let dur = m.duration;
    if (typeof dur === 'string') { const mm = dur.match(/(\d+)/); dur = mm ? parseInt(mm[1]) : 7; }
    if (typeof dur !== 'number' || isNaN(dur)) dur = 7;
    let instr = m.instructions;
    if (typeof instr === 'string') instr = instr.trim() ? [instr.trim()] : [];
    if (!Array.isArray(instr)) instr = [];
    return { id: m.id || i + 1, ...m, duration: dur, instructions: instr };
  };

  const [meds, setMeds] = useState(
    initialRx?.meds.map((m, i) => normaliseMed(m, i))
    || [{ id: 1, drug: 'Amlodipine', form: 'Tablet', strength: '5mg', qty: 1, freq: 'OD', duration: 30, timing: 'Morning', instructions: ['After meals'] }]
  );
  const [investigations, setInvestigations] = useState(initialRx?.investigations || []);
  const [showInvPicker, setShowInvPicker] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const [showInstrPicker, setShowInstrPicker] = useState(null); // medId or null
  const [printed, setPrinted] = useState(false);
  const [historyTab, setHistoryTab] = useState('chart');
  const [selectedVisit, setSelectedVisit] = useState(null); // { visit, idx } | null

  const interactions = checkInteractions(meds);

  const addMed = (drug) => {
    setMeds(ms => [...ms, {
      id: Date.now(), drug: drug.name, form: drug.form, strength: drug.strengths[0],
      qty: 1, freq: 'OD', duration: 7, timing: 'Morning', instructions: [],
    }]);
  };
  const removeMed = (id) => setMeds(ms => ms.filter(m => m.id !== id));
  const updateMed = (id, key, val) => setMeds(ms => ms.map(m => m.id === id ? { ...m, [key]: val } : m));
  const toggleInstrFor = (medId, txt) => setMeds(ms => ms.map(m => m.id === medId
    ? { ...m, instructions: (m.instructions || []).includes(txt) ? m.instructions.filter(x => x !== txt) : [...(m.instructions || []), txt] }
    : m));

  const toggleInv = (inv) => setInvestigations(invs => invs.includes(inv) ? invs.filter(i => i !== inv) : [...invs, inv]);

  const handleFinalise = () => {
    setPrinted(true);
    setTimeout(() => setPrinted(false), 2500);
  };

  const handlePrint = () => {
    const rxForPrint = {
      id: initialRx?.id || 'RX-' + Date.now(),
      uhid: patient.uhid, name: patient.name, age: patient.age, gender: patient.gender === 'Male' ? 'M' : (patient.gender === 'Female' ? 'F' : patient.gender),
      date: new Date().toISOString().slice(0, 10), time: new Date().toTimeString().slice(0, 5),
      vitals, diagnosis, meds, investigations, followup: followup || '', notes, lang,
    };
    // Stash in window and route
    window.__pendingPrintRx = rxForPrint;
    onNav('print-rx', { rxId: rxForPrint.id, inline: rxForPrint });
  };

  const freqOptions = ['OD', 'BD', 'TDS', 'QID', 'SOS', 'OW', 'BW', 'HS', 'Q4H', 'Q6H'];
  const timingOptions = ['Morning', 'Afternoon', 'Evening', 'Night', 'Morning & Night', 'Morning, Afternoon & Night', 'Before meals', 'After meals'];
  // Find drug record for strength options (when added from DRUG_DB)
  const drugRecordFor = (name) => DRUG_DB.find(d => d.name === name);
  // Pick a form icon
  const formIcon = (form) => ({
    'Tablet': '💊', 'Capsule': '💊', 'Syrup': '🧴', 'Injection': '💉',
    'Eye Drop': '👁️', 'Ear Drop': '👂', 'Nasal Drop': '👃',
    'Cream': '🧴', 'Ointment': '🧴', 'Inhaler': '🌬️', 'Powder': '🥄', 'Suppository': '💊',
  }[form] || '💊');

  const langLabel = { en: 'English', hi: 'हिंदी', mr: 'मराठी' };
  const visits = history.visits;
  const hasBP = visits.some(v => v.bp_sys);
  const hasSugar = visits.some(v => v.sugar);

  return (
    <div className="fade-in" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 20 }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <button onClick={() => onNav('prescriptions-list')} style={{ background: 'none', border: 'none', color: CC.muted, cursor: 'pointer', fontSize: 14 }}>← Back</button>
          <div>
            <div style={{ fontSize: 18, fontWeight: 800 }}>{initialRx ? `Edit Prescription · ${initialRx.id}` : 'New Prescription'}</div>
            <div style={{ fontSize: 13, color: CC.muted }}>OPD Consultation · Remarks in <strong style={{ color: CC.primary }}>{langLabel[lang]}</strong></div>
          </div>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <Btn variant="ghost" onClick={() => setShowSettings(true)}>⚙️ Settings</Btn>
          <Btn variant="ghost" onClick={handlePrint}>🖨️ Print</Btn>
          <Btn onClick={handleFinalise}>✅ Save & Finalise</Btn>
        </div>
      </div>

      {printed && (
        <div style={{ background: '#ECFDF5', border: `1px solid ${CC.success}`, borderRadius: 10, padding: '12px 18px', display: 'flex', alignItems: 'center', gap: 10, color: CC.success, fontWeight: 600 }}>
          ✅ Prescription saved and sent to pharmacy queue!
        </div>
      )}

      {interactions.length > 0 && (
        <div style={{ background: '#FFFBEB', border: `1px solid ${CC.warning}`, borderRadius: 10, padding: '12px 18px' }}>
          <div style={{ fontWeight: 700, color: CC.warning, marginBottom: 6, fontSize: 13 }}>⚠️ Drug Interaction Alert</div>
          {interactions.map((msg, i) => <div key={i} style={{ fontSize: 13, color: '#92400E' }}>{msg}</div>)}
        </div>
      )}

      {/* Patient banner */}
      <Card>
        <div style={{ padding: '16px 20px', background: `linear-gradient(135deg,${CC.navy},#1565C0)`, borderRadius: '14px 14px 0 0' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ display: 'flex', gap: 14, alignItems: 'center' }}>
              <div style={{ width: 48, height: 48, borderRadius: 12, background: 'rgba(255,255,255,0.2)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontWeight: 800, fontSize: 18 }}>
                {patient.name.split(' ').map(n => n[0]).join('')}
              </div>
              <div>
                <div style={{ color: '#fff', fontWeight: 800, fontSize: 16 }}>{patient.name}</div>
                <div style={{ color: 'rgba(255,255,255,0.7)', fontSize: 12 }}>{patient.uhid} · {patient.age}y · {patient.gender} · {patient.blood}</div>
              </div>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div style={{ color: 'rgba(255,255,255,0.7)', fontSize: 11 }}>⚠️ Allergies: <strong style={{ color: '#FCA5A5' }}>{patient.allergies}</strong></div>
              <div style={{ color: 'rgba(255,255,255,0.7)', fontSize: 11 }}>Chronic: {patient.chronic.join(', ')}</div>
              <div style={{ color: 'rgba(255,255,255,0.6)', fontSize: 11, marginTop: 2 }}>{patient.date}</div>
            </div>
          </div>
        </div>
        <div style={{ padding: '16px 20px' }}>
          <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 12, color: CC.text }}>Today's Vitals</div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7,1fr)', gap: 10 }}>
            {[
              { key: 'bp', label: 'BP (mmHg)', ph: '120/80', icon: '❤️' },
              { key: 'pulse', label: 'Pulse', ph: '72', icon: '💓' },
              { key: 'temp', label: 'Temp °F', ph: '98.6', icon: '🌡️' },
              { key: 'weight', label: 'Weight kg', ph: '70', icon: '⚖️' },
              { key: 'spo2', label: 'SpO₂ %', ph: '98', icon: '🫁' },
              { key: 'rr', label: 'RR /min', ph: '16', icon: '🫧' },
              { key: 'sugar', label: 'Sugar (F)', ph: '95', icon: '🩸' },
            ].map(v => (
              <div key={v.key} style={{ background: '#F8FAFC', borderRadius: 10, padding: '10px', textAlign: 'center' }}>
                <div style={{ fontSize: 14, marginBottom: 2 }}>{v.icon}</div>
                <input value={vitals[v.key] || ''} onChange={e => setVitals(vs => ({ ...vs, [v.key]: e.target.value }))} placeholder={v.ph}
                  style={{ width: '100%', border: 'none', background: 'transparent', textAlign: 'center', fontSize: 14, fontWeight: 700, outline: 'none', color: CC.text }} />
                <div style={{ fontSize: 9, color: CC.muted, marginTop: 2 }}>{v.label}</div>
              </div>
            ))}
          </div>
        </div>
      </Card>

      {/* Patient History + Charts */}
      <Card>
        <div style={{ padding: '14px 20px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderBottom: `1px solid ${CC.border}` }}>
          <div style={{ fontWeight: 700, fontSize: 14 }}>📊 Patient History & Trends</div>
          <div style={{ display: 'flex', gap: 4, background: '#F1F5F9', borderRadius: 8, padding: 3 }}>
            {[['chart','Charts'], ['visits','Visit History'], ['summary','Summary']].map(([id, label]) => (
              <button key={id} onClick={() => setHistoryTab(id)} style={{ padding: '5px 12px', borderRadius: 6, border: 'none', background: historyTab === id ? '#fff' : 'transparent', fontWeight: historyTab === id ? 700 : 500, fontSize: 11, color: historyTab === id ? CC.primary : CC.muted, cursor: 'pointer', boxShadow: historyTab === id ? '0 1px 3px rgba(0,0,0,0.1)' : 'none' }}>{label}</button>
            ))}
          </div>
        </div>

        {historyTab === 'chart' && (
          <div style={{ padding: 16, display: 'grid', gridTemplateColumns: (hasBP && hasSugar) ? '1fr 1fr' : '1fr', gap: 14 }}>
            {hasBP && <VitalsChart visits={visits} metric="bp" />}
            {hasSugar && <VitalsChart visits={visits} metric="sugar" />}
            {!hasBP && !hasSugar && <div style={{ padding: 30, textAlign: 'center', color: CC.muted }}>No BP/Sugar history recorded.</div>}
          </div>
        )}

        {historyTab === 'visits' && (
          <div style={{ padding: '0' }}>
            <Table
              cols={['Date', 'Diagnosis', 'BP', 'Sugar', 'Weight', '']}
              rows={[...visits].map((v, idx) => ({ v, originalIdx: idx })).reverse().map(({ v, originalIdx }) => ({ cells: [
                <span style={{ fontWeight: 600 }}>{v.date}</span>,
                <span style={{ fontSize: 12 }}>{v.dx}</span>,
                <span style={{ fontWeight: 600 }}>{v.bp_sys}/{v.bp_dia}</span>,
                <span style={{ fontWeight: 600 }}>{v.sugar || '—'}</span>,
                <span>{v.weight} kg</span>,
                <button
                  onClick={() => setSelectedVisit({ visit: v, idx: originalIdx })}
                  style={{
                    padding: '5px 12px', borderRadius: 6, border: `1.5px solid ${CC.primary}`,
                    background: '#fff', color: CC.primary, fontSize: 11, fontWeight: 700,
                    cursor: 'pointer', display: 'inline-flex', alignItems: 'center', gap: 4,
                    transition: 'all 0.15s',
                  }}
                  onMouseEnter={e => { e.currentTarget.style.background = CC.sky; }}
                  onMouseLeave={e => { e.currentTarget.style.background = '#fff'; }}
                >👁 View</button>,
              ]}))}
            />
          </div>
        )}

        {historyTab === 'summary' && (
          <div style={{ padding: 16, display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 12 }}>
            <div style={{ padding: 14, background: '#F0F7FF', borderRadius: 10 }}>
              <div style={{ fontSize: 11, color: CC.muted, fontWeight: 600 }}>TOTAL VISITS</div>
              <div style={{ fontSize: 24, fontWeight: 800, color: CC.primary }}>{visits.length}</div>
              <div style={{ fontSize: 11, color: CC.muted }}>Last: {visits[visits.length-1].date}</div>
            </div>
            <div style={{ padding: 14, background: '#FEF2F2', borderRadius: 10 }}>
              <div style={{ fontSize: 11, color: CC.muted, fontWeight: 600 }}>BP TREND</div>
              <div style={{ fontSize: 24, fontWeight: 800, color: CC.error }}>{visits[visits.length-1].bp_sys}/{visits[visits.length-1].bp_dia}</div>
              <div style={{ fontSize: 11, color: CC.success }}>↓ {visits[0].bp_sys - visits[visits.length-1].bp_sys} mmHg since baseline</div>
            </div>
            <div style={{ padding: 14, background: '#F5F3FF', borderRadius: 10 }}>
              <div style={{ fontSize: 11, color: CC.muted, fontWeight: 600 }}>SUGAR TREND</div>
              <div style={{ fontSize: 24, fontWeight: 800, color: '#7C3AED' }}>{visits[visits.length-1].sugar || '—'}</div>
              {visits[0].sugar && visits[visits.length-1].sugar && <div style={{ fontSize: 11, color: CC.success }}>↓ {visits[0].sugar - visits[visits.length-1].sugar} mg/dL</div>}
            </div>
          </div>
        )}
      </Card>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 320px', gap: 20 }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {/* Diagnosis */}
          <Card style={{ padding: 20 }}>
            <div style={{ fontWeight: 700, fontSize: 14, marginBottom: 12 }}>Diagnosis / Clinical Notes</div>
            <textarea value={diagnosis} onChange={e => setDiagnosis(e.target.value)} rows={2} placeholder="Primary diagnosis, ICD code, clinical impression…"
              style={{ width: '100%', padding: '10px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, fontFamily: 'inherit', resize: 'vertical', outline: 'none' }} />
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 10 }}>
              {['Hypertension', 'Type 2 DM', 'CAD', 'GERD', 'URTI', 'Anaemia', 'Hypothyroid'].map(d => (
                <button key={d} onClick={() => setDiagnosis(prev => prev ? `${prev}, ${d}` : d)}
                  style={{ padding: '4px 10px', borderRadius: 14, border: `1px solid ${CC.border}`, background: '#fff', fontSize: 11, cursor: 'pointer', color: CC.text }}>+ {d}</button>
              ))}
            </div>
          </Card>

          {/* Medicines */}
          <Card title="℞ Medicines Prescribed" action={<Badge color={CC.primary} bg={CC.sky}>{meds.length} drug{meds.length !== 1 ? 's' : ''}</Badge>}>
            <div style={{ padding: '0 20px 16px' }}>
              {meds.length === 0 && <div style={{ textAlign: 'center', padding: 24, color: CC.muted, fontSize: 13 }}>No medicines added yet. Use the search below.</div>}
              {meds.map((med, i) => {
                const remark = generateRemark(med, lang);
                const drugRec = drugRecordFor(med.drug);
                const strengthOptions = drugRec?.strengths || [med.strength];
                return (
                  <div key={med.id} style={{ borderRadius: 12, border: `1.5px solid ${CC.border}`, marginBottom: 12, overflow: 'hidden', background: '#fff' }}>
                    {/* Row 1: Drug name + form badge + remove */}
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '12px 14px 10px', background: '#F8FAFC', borderBottom: `1px solid ${CC.border}` }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 10, flex: 1, minWidth: 0 }}>
                        <div style={{ width: 30, height: 30, borderRadius: 8, background: CC.sky, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 16, flexShrink: 0 }}>{formIcon(med.form)}</div>
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                            <span style={{ fontWeight: 700, fontSize: 14 }}>{i+1}. {med.drug}</span>
                            <span style={{ fontSize: 10, fontWeight: 700, color: CC.muted, padding: '2px 8px', background: '#fff', border: `1px solid ${CC.border}`, borderRadius: 4, textTransform: 'uppercase', letterSpacing: 0.4 }}>{med.form}</span>
                          </div>
                        </div>
                      </div>
                      <button onClick={() => removeMed(med.id)} title="Remove" style={{ background: 'none', border: 'none', cursor: 'pointer', color: CC.error, fontSize: 16, padding: '4px 8px', borderRadius: 6 }}
                        onMouseEnter={e => e.currentTarget.style.background = '#FEF2F2'}
                        onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
                      >🗑️</button>
                    </div>

                    {/* Row 2: Strength · Quantity · Duration · Timing */}
                    <div style={{ padding: '12px 14px', display: 'grid', gridTemplateColumns: '1.2fr 0.8fr 1.1fr 1.5fr', gap: 10, alignItems: 'end', borderBottom: `1px solid ${CC.border}` }}>
                      <div>
                        <div style={{ fontSize: 10, fontWeight: 700, color: CC.muted, marginBottom: 4, textTransform: 'uppercase', letterSpacing: 0.4 }}>Strength</div>
                        {strengthOptions.length > 1 ? (
                          <select value={med.strength} onChange={e => updateMed(med.id, 'strength', e.target.value)} style={{ width: '100%', padding: '8px 8px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, fontWeight: 600 }}>
                            {strengthOptions.map(s => <option key={s}>{s}</option>)}
                          </select>
                        ) : (
                          <input value={med.strength} onChange={e => updateMed(med.id, 'strength', e.target.value)} style={{ width: '100%', padding: '8px 10px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, fontWeight: 600, outline: 'none' }} />
                        )}
                      </div>
                      <div>
                        <div style={{ fontSize: 10, fontWeight: 700, color: CC.muted, marginBottom: 4, textTransform: 'uppercase', letterSpacing: 0.4 }}>Quantity</div>
                        <input type="number" min={1} value={med.qty || 1} onChange={e => updateMed(med.id, 'qty', Math.max(1, parseInt(e.target.value) || 1))} style={{ width: '100%', padding: '8px 10px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, fontWeight: 700, textAlign: 'center', outline: 'none' }} />
                      </div>
                      <div>
                        <div style={{ fontSize: 10, fontWeight: 700, color: CC.muted, marginBottom: 4, textTransform: 'uppercase', letterSpacing: 0.4 }}>Duration (Days)</div>
                        <div style={{ position: 'relative' }}>
                          <input type="number" min={1} value={med.duration} onChange={e => updateMed(med.id, 'duration', Math.max(1, parseInt(e.target.value) || 1))} style={{ width: '100%', padding: '8px 32px 8px 10px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, fontWeight: 700, outline: 'none' }} />
                          <span style={{ position: 'absolute', right: 10, top: '50%', transform: 'translateY(-50%)', fontSize: 11, color: CC.muted, fontWeight: 600 }}>days</span>
                        </div>
                      </div>
                      <div>
                        <div style={{ fontSize: 10, fontWeight: 700, color: CC.muted, marginBottom: 4, textTransform: 'uppercase', letterSpacing: 0.4 }}>Timing</div>
                        <select value={med.timing} onChange={e => updateMed(med.id, 'timing', e.target.value)} style={{ width: '100%', padding: '8px 8px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13 }}>
                          {timingOptions.map(t => <option key={t}>{t}</option>)}
                        </select>
                      </div>
                    </div>

                    {/* Row 3: Frequency segmented control */}
                    <div style={{ padding: '12px 14px', borderBottom: `1px solid ${CC.border}` }}>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                        <div style={{ fontSize: 10, fontWeight: 700, color: CC.muted, textTransform: 'uppercase', letterSpacing: 0.4 }}>How often (Frequency)</div>
                        <div style={{ fontSize: 10, color: CC.muted, fontStyle: 'italic' }}>e.g. once a day, twice a day…</div>
                      </div>
                      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6,1fr)', gap: 6 }}>
                        {FREQUENCY_PRESETS.map(f => {
                          const selected = med.freq === f.code;
                          return (
                            <button key={f.code} onClick={() => updateMed(med.id, 'freq', f.code)} style={{
                              padding: '8px 6px', borderRadius: 8,
                              border: `1.5px solid ${selected ? CC.primary : CC.border}`,
                              background: selected ? CC.sky : '#fff',
                              color: selected ? CC.primary : CC.text,
                              cursor: 'pointer', textAlign: 'center', transition: 'all 0.15s',
                            }}>
                              <div style={{ fontSize: 12, fontWeight: 700 }}>{f.label}</div>
                              <div style={{ fontSize: 9, color: selected ? CC.primary : CC.muted, marginTop: 2, fontWeight: 600 }}>{f.pattern}</div>
                            </button>
                          );
                        })}
                      </div>
                    </div>

                    {/* Row 4: Instructions chips */}
                    <div style={{ padding: '12px 14px', borderBottom: `1px solid ${CC.border}` }}>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
                        <div style={{ fontSize: 10, fontWeight: 700, color: CC.muted, textTransform: 'uppercase', letterSpacing: 0.4 }}>Instructions</div>
                        <button onClick={() => setShowInstrPicker(med.id)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: CC.primary, fontSize: 11, fontWeight: 700 }}>+ Add from library</button>
                      </div>
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, minHeight: 24 }}>
                        {(med.instructions || []).length === 0 && <span style={{ fontSize: 12, color: CC.muted, fontStyle: 'italic' }}>No instructions added — click "Add from library" or use quick chips below.</span>}
                        {(med.instructions || []).map((inst, idx) => (
                          <div key={idx} style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '4px 10px', background: '#FFFBEB', borderRadius: 14, border: '1px solid #FDE68A' }}>
                            <span style={{ fontSize: 11, fontWeight: 600, color: '#92400E' }}>📌 {inst}</span>
                            <button onClick={() => toggleInstrFor(med.id, inst)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#92400E', fontSize: 10 }}>✕</button>
                          </div>
                        ))}
                      </div>
                      {/* Quick chips */}
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 5, marginTop: 8 }}>
                        {['After meals', 'Before meals', 'On empty stomach', 'With a full glass of water', 'Avoid alcohol'].map(q => (
                          !(med.instructions || []).includes(q) && (
                            <button key={q} onClick={() => toggleInstrFor(med.id, q)} style={{ padding: '3px 9px', borderRadius: 12, border: `1px dashed ${CC.border}`, background: '#fff', fontSize: 11, cursor: 'pointer', color: CC.muted }}>+ {q}</button>
                          )
                        ))}
                      </div>
                    </div>

                    {/* AUTO-GENERATED REMARK */}
                    <div style={{ padding: '10px 14px', background: 'linear-gradient(90deg, #EFF6FF, #F0F7FF)', display: 'flex', gap: 10, alignItems: 'flex-start' }}>
                      <span style={{ fontSize: 11, fontWeight: 700, color: CC.primary, textTransform: 'uppercase', letterSpacing: 0.5, minWidth: 70 }}>📝 Remark</span>
                      <div style={{ flex: 1, fontSize: 13, color: CC.text, fontStyle: 'italic', lineHeight: 1.5 }}>{remark}</div>
                      <span style={{ fontSize: 9, color: CC.muted, fontWeight: 700, padding: '2px 6px', background: '#fff', borderRadius: 4 }}>{langLabel[lang]}</span>
                    </div>
                  </div>
                );
              })}
              <div style={{ marginTop: 4 }}>
                <div style={{ fontSize: 12, fontWeight: 600, color: CC.muted, marginBottom: 6 }}>ADD MEDICINE</div>
                <DrugSearch onSelect={addMed} />
              </div>
            </div>
          </Card>

          {/* Investigations */}
          <Card title="🔬 Investigations" action={<Btn size="sm" variant="light" onClick={() => setShowInvPicker(true)}>+ Add from list</Btn>}>
            <div style={{ padding: '0 20px 16px' }}>
              {investigations.length === 0 && <div style={{ padding: 16, color: CC.muted, fontSize: 13, textAlign: 'center' }}>No investigations selected.</div>}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                {investigations.map((inv, i) => (
                  <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '6px 12px', background: '#F5F3FF', borderRadius: 20, border: '1px solid #DDD6FE' }}>
                    <span style={{ fontSize: 12, fontWeight: 600, color: '#7C3AED' }}>🔬 {inv}</span>
                    <button onClick={() => toggleInv(inv)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#9CA3AF', fontSize: 12 }}>✕</button>
                  </div>
                ))}
              </div>
            </div>
          </Card>
        </div>

        {/* Right column */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <Card style={{ padding: 20 }}>
            <div style={{ fontWeight: 700, fontSize: 14, marginBottom: 14 }}>📅 Next Visit</div>
            <FormField label="Follow-up Date">
              <Input type="date" value={followup} onChange={e => setFollowup(e.target.value)} />
            </FormField>
            {followup && (
              <div style={{ padding: '10px 12px', background: CC.sky, borderRadius: 8, marginTop: 4 }}>
                <div style={{ fontSize: 10, color: CC.muted, fontWeight: 700 }}>SCHEDULED</div>
                <div style={{ fontSize: 13, fontWeight: 700, color: CC.primary }}>{new Date(followup).toLocaleDateString('en-IN', { dateStyle: 'long' })}</div>
              </div>
            )}
            <div style={{ marginTop: 14 }}>
              <FormField label="General Advice">
                <textarea value={notes} onChange={e => setNotes(e.target.value)} rows={4} placeholder="Diet advice, lifestyle, restrictions…"
                  style={{ width: '100%', padding: '9px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, fontFamily: 'inherit', resize: 'vertical', outline: 'none' }} />
              </FormField>
            </div>
          </Card>

          <Card title="Last Visit" style={{ overflow: 'hidden' }}>
            <div style={{ padding: '0 20px 16px' }}>
              <div style={{ fontSize: 12, color: CC.muted, marginBottom: 4 }}>{visits[visits.length-1].date}</div>
              <div style={{ fontWeight: 700, fontSize: 13 }}>{visits[visits.length-1].dx}</div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginTop: 10 }}>
                <div style={{ padding: '8px 10px', background: '#FEF2F2', borderRadius: 8 }}>
                  <div style={{ fontSize: 10, color: CC.muted, fontWeight: 600 }}>BP</div>
                  <div style={{ fontSize: 15, fontWeight: 800, color: CC.error }}>{visits[visits.length-1].bp_sys}/{visits[visits.length-1].bp_dia}</div>
                </div>
                <div style={{ padding: '8px 10px', background: '#F5F3FF', borderRadius: 8 }}>
                  <div style={{ fontSize: 10, color: CC.muted, fontWeight: 600 }}>SUGAR</div>
                  <div style={{ fontSize: 15, fontWeight: 800, color: '#7C3AED' }}>{visits[visits.length-1].sugar || '—'}</div>
                </div>
              </div>
            </div>
          </Card>

          <Card title="Rx Preview" style={{ background: `linear-gradient(160deg,${CC.navy},#1565C0)` }}>
            <div style={{ padding: '0 20px 20px', maxHeight: 280, overflowY: 'auto' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                {meds.map((m, i) => (
                  <div key={i} style={{ background: 'rgba(255,255,255,0.08)', borderRadius: 8, padding: '8px 12px' }}>
                    <div style={{ color: '#fff', fontWeight: 700, fontSize: 12 }}>{i+1}. {m.drug} {m.strength}</div>
                    <div style={{ color: 'rgba(255,255,255,0.6)', fontSize: 10, fontStyle: 'italic' }}>{generateRemark(m, lang)}</div>
                  </div>
                ))}
                {meds.length === 0 && <div style={{ color: 'rgba(255,255,255,0.4)', fontSize: 13, textAlign: 'center', padding: 8 }}>No medicines yet</div>}
              </div>
            </div>
          </Card>
        </div>
      </div>

      {/* Settings Modal */}
      <Modal open={showSettings} onClose={() => setShowSettings(false)} title="Prescription Settings" width={460}>
        <div style={{ padding: '4px 0' }}>
          <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 8 }}>Remark Language</div>
          <div style={{ fontSize: 12, color: CC.muted, marginBottom: 14 }}>Auto-generated remarks (dosage instructions) will appear in this language on the prescription.</div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 10 }}>
            {[
              { id: 'en', label: 'English', sample: 'Take 1 tablet twice a day, after meals' },
              { id: 'hi', label: 'हिंदी (Hindi)', sample: '1 गोली दिन में दो बार, भोजन के बाद लें' },
              { id: 'mr', label: 'मराठी (Marathi)', sample: '1 गोळी दिवसातून दोनदा, जेवणानंतर घ्या' },
            ].map(l => (
              <button key={l.id} onClick={() => setLanguage(l.id)} style={{
                padding: 12, borderRadius: 10, border: `2px solid ${lang === l.id ? CC.primary : CC.border}`,
                background: lang === l.id ? CC.sky : '#fff', cursor: 'pointer', textAlign: 'left',
              }}>
                <div style={{ fontWeight: 700, fontSize: 13, color: lang === l.id ? CC.primary : CC.text, marginBottom: 4 }}>{l.label}</div>
                <div style={{ fontSize: 10, color: CC.muted, lineHeight: 1.4, fontStyle: 'italic' }}>{l.sample}</div>
              </button>
            ))}
          </div>
          <div style={{ marginTop: 16, padding: 12, background: '#F8FAFC', borderRadius: 8, fontSize: 11, color: CC.muted, lineHeight: 1.5 }}>
            💡 Default is English. Doctor can change at any time — all remarks update instantly.
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16 }}>
            <Btn onClick={() => setShowSettings(false)}>Done</Btn>
          </div>
        </div>
      </Modal>

      {/* Investigations Picker */}
      <Modal open={showInvPicker} onClose={() => setShowInvPicker(false)} title="Add Investigations" width={620}>
        <div style={{ maxHeight: 480, overflowY: 'auto' }}>
          {Object.entries(INVESTIGATIONS_MASTER).map(([category, list]) => (
            <div key={category} style={{ marginBottom: 16 }}>
              <div style={{ fontWeight: 700, fontSize: 12, color: CC.muted, textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 8 }}>{category}</div>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                {list.map(inv => {
                  const selected = investigations.includes(inv);
                  return (
                    <button key={inv} onClick={() => toggleInv(inv)} style={{
                      padding: '6px 12px', borderRadius: 18, border: `1.5px solid ${selected ? '#7C3AED' : CC.border}`,
                      background: selected ? '#F5F3FF' : '#fff', color: selected ? '#7C3AED' : CC.text,
                      fontSize: 12, fontWeight: selected ? 700 : 500, cursor: 'pointer',
                    }}>{selected ? '✓ ' : '+ '}{inv}</button>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 14, paddingTop: 14, borderTop: `1px solid ${CC.border}` }}>
          <div style={{ fontSize: 13, color: CC.muted }}>{investigations.length} selected</div>
          <Btn onClick={() => setShowInvPicker(false)}>Done</Btn>
        </div>
      </Modal>

      {/* Instructions Picker (per-medicine) */}
      <Modal open={!!showInstrPicker} onClose={() => setShowInstrPicker(null)} title="Add Instructions" width={580}>
        {(() => {
          const med = meds.find(m => m.id === showInstrPicker);
          if (!med) return null;
          const current = med.instructions || [];
          return (
            <>
              <div style={{ padding: '10px 14px', background: CC.sky, borderRadius: 10, marginBottom: 16, display: 'flex', alignItems: 'center', gap: 10 }}>
                <span style={{ fontSize: 18 }}>{formIcon(med.form)}</span>
                <div>
                  <div style={{ fontSize: 13, fontWeight: 700 }}>{med.drug} {med.strength}</div>
                  <div style={{ fontSize: 11, color: CC.muted }}>Choose from the templates below — multiple allowed</div>
                </div>
              </div>
              <div style={{ maxHeight: 420, overflowY: 'auto', paddingRight: 4 }}>
                {Object.entries(INSTRUCTION_TEMPLATES).map(([category, list]) => (
                  <div key={category} style={{ marginBottom: 14 }}>
                    <div style={{ fontWeight: 700, fontSize: 11, color: CC.muted, textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 6 }}>{category}</div>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                      {list.map(inst => {
                        const selected = current.includes(inst);
                        return (
                          <button key={inst} onClick={() => toggleInstrFor(med.id, inst)} style={{
                            padding: '6px 12px', borderRadius: 18,
                            border: `1.5px solid ${selected ? CC.warning : CC.border}`,
                            background: selected ? '#FFFBEB' : '#fff',
                            color: selected ? '#92400E' : CC.text,
                            fontSize: 12, fontWeight: selected ? 700 : 500, cursor: 'pointer', transition: 'all 0.15s',
                          }}>{selected ? '✓ ' : '+ '}{inst}</button>
                        );
                      })}
                    </div>
                  </div>
                ))}
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 14, paddingTop: 14, borderTop: `1px solid ${CC.border}` }}>
                <div style={{ fontSize: 13, color: CC.muted }}>{current.length} selected</div>
                <Btn onClick={() => setShowInstrPicker(null)}>Done</Btn>
              </div>
            </>
          );
        })()}
      </Modal>

      {/* Visit Detail Modal — smart popup with trend deltas + related Rx */}
      <VisitDetailModal
        open={!!selectedVisit}
        onClose={() => setSelectedVisit(null)}
        visit={selectedVisit?.visit}
        idx={selectedVisit?.idx ?? 0}
        visits={visits}
        patient={patient}
      />
    </div>
  );
}

// ─── Visit Detail Modal ────────────────────────────────────────────────────
function VisitDetailModal({ open, onClose, visit, idx, visits, patient }) {
  if (!open || !visit) return null;

  // Find related prescription by uhid + date
  const relatedRx = PRESCRIPTIONS_DB.find(rx => rx.uhid === patient.uhid && rx.date === visit.date);

  // Trend vs previous visit (idx is into chronological visits array; idx > 0 means there's a previous)
  const prev = idx > 0 ? visits[idx - 1] : null;
  const delta = (cur, prv) => {
    if (cur == null || prv == null) return null;
    const d = cur - prv;
    return { value: d, abs: Math.abs(d), dir: d > 0 ? 'up' : d < 0 ? 'down' : 'flat' };
  };
  const bpSysΔ   = prev ? delta(visit.bp_sys, prev.bp_sys) : null;
  const bpDiaΔ   = prev ? delta(visit.bp_dia, prev.bp_dia) : null;
  const sugarΔ   = prev && visit.sugar && prev.sugar ? delta(visit.sugar, prev.sugar) : null;
  const weightΔ  = prev ? delta(visit.weight, prev.weight) : null;

  // For BP/sugar/weight, "down" is generally good (green); for nothing else
  const trendColor = (d, lowerIsBetter = true) => {
    if (!d || d.dir === 'flat') return CC.muted;
    if (lowerIsBetter) return d.dir === 'down' ? CC.success : CC.error;
    return d.dir === 'up' ? CC.success : CC.error;
  };
  const trendArrow = (d) => d?.dir === 'up' ? '↑' : d?.dir === 'down' ? '↓' : '→';

  // BP classification
  const bpClass = (sys, dia) => {
    if (sys >= 140 || dia >= 90) return { label: 'Stage 2 HTN', color: CC.error, bg: '#FEF2F2' };
    if (sys >= 130 || dia >= 80) return { label: 'Stage 1 HTN', color: CC.warning, bg: '#FFFBEB' };
    if (sys >= 120) return { label: 'Elevated', color: CC.warning, bg: '#FFFBEB' };
    return { label: 'Normal', color: CC.success, bg: '#ECFDF5' };
  };
  const bp = bpClass(visit.bp_sys, visit.bp_dia);

  // Sugar classification (fasting)
  const sugarClass = (s) => {
    if (!s) return null;
    if (s >= 126) return { label: 'Diabetic range', color: CC.error, bg: '#FEF2F2' };
    if (s >= 100) return { label: 'Pre-diabetic', color: CC.warning, bg: '#FFFBEB' };
    return { label: 'Normal', color: CC.success, bg: '#ECFDF5' };
  };
  const sg = sugarClass(visit.sugar);

  const formattedDate = new Date(visit.date).toLocaleDateString('en-IN', { weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' });
  const visitNumber = idx + 1; // 1-based

  // ─── Medicines: prefer visit.meds, fall back to a matching prescription ────
  const meds = visit.meds || relatedRx?.meds || [];
  const prevMeds = prev?.meds || [];
  const notes = visit.notes || relatedRx?.notes || '';
  const investigations = visit.investigations || relatedRx?.investigations || [];

  // Diff each med vs previous visit
  const medStatus = (m) => {
    if (!prev) return null;
    const prior = prevMeds.find(p => p.drug.toLowerCase() === m.drug.toLowerCase());
    if (!prior) return { kind: 'new', label: 'NEW', color: CC.success, bg: '#ECFDF5' };
    if (prior.strength !== m.strength || prior.freq !== m.freq) {
      return { kind: 'changed', label: 'CHANGED', color: CC.warning, bg: '#FFFBEB', from: `${prior.strength} · ${prior.freq}`, to: `${m.strength} · ${m.freq}` };
    }
    return { kind: 'continued', label: 'CONTINUED', color: CC.muted, bg: '#F1F5F9' };
  };
  // Drugs that were in prev but not in current = stopped
  const stoppedMeds = prevMeds.filter(p => !meds.some(m => m.drug.toLowerCase() === p.drug.toLowerCase()));

  const handleOpenNewTab = () => {
    const html = buildStandaloneVisitHTML({ visit, idx, visits, patient, relatedRx, formattedDate, visitNumber, bp, sg, prev, bpSysΔ, bpDiaΔ, sugarΔ, weightΔ, meds, prevMeds, notes, investigations, stoppedMeds });
    const win = window.open('', '_blank');
    if (win) {
      win.document.open();
      win.document.write(html);
      win.document.close();
    }
  };

  return (
    <Modal open={open} onClose={onClose} title="" width={720}>
      <div style={{ marginTop: -8 }}>
        {/* Header strip */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 18 }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
              <span style={{ fontSize: 11, fontWeight: 700, color: CC.primary, background: CC.sky, padding: '3px 10px', borderRadius: 20, letterSpacing: 0.4 }}>VISIT #{visitNumber} of {visits.length}</span>
              {idx === visits.length - 1 && <span style={{ fontSize: 10, fontWeight: 700, color: CC.success, background: '#ECFDF5', padding: '3px 8px', borderRadius: 20 }}>MOST RECENT</span>}
            </div>
            <div style={{ fontSize: 20, fontWeight: 800, color: CC.text, lineHeight: 1.2 }}>{visit.dx}</div>
            <div style={{ fontSize: 12, color: CC.muted, marginTop: 4 }}>📅 {formattedDate}</div>
          </div>
          <button onClick={handleOpenNewTab} title="Open in new tab"
            style={{ padding: '7px 12px', borderRadius: 8, border: `1.5px solid ${CC.border}`, background: '#fff', color: CC.text, fontSize: 11, fontWeight: 600, cursor: 'pointer', display: 'inline-flex', alignItems: 'center', gap: 5 }}
            onMouseEnter={e => { e.currentTarget.style.background = '#F8FAFC'; e.currentTarget.style.borderColor = CC.primary; e.currentTarget.style.color = CC.primary; }}
            onMouseLeave={e => { e.currentTarget.style.background = '#fff'; e.currentTarget.style.borderColor = CC.border; e.currentTarget.style.color = CC.text; }}
          >↗ Open in new tab</button>
        </div>

        {/* Patient mini-banner */}
        <div style={{ padding: '10px 14px', background: '#F8FAFC', borderRadius: 10, marginBottom: 18, display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{ width: 36, height: 36, borderRadius: 10, background: `linear-gradient(135deg,${CC.primary},${CC.mid})`, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 800, fontSize: 13 }}>
            {patient.name.split(' ').map(n => n[0]).join('')}
          </div>
          <div style={{ flex: 1 }}>
            <div style={{ fontSize: 13, fontWeight: 700 }}>{patient.name}</div>
            <div style={{ fontSize: 11, color: CC.muted }}>{patient.uhid} · {patient.age}y · {patient.gender} · {patient.blood}</div>
          </div>
          <div style={{ fontSize: 10, color: CC.muted, textAlign: 'right' }}>
            <div>Allergies: <strong style={{ color: CC.error }}>{patient.allergies}</strong></div>
            <div>Chronic: {patient.chronic.join(', ')}</div>
          </div>
        </div>

        {/* Vitals grid with trend deltas */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 10, marginBottom: 18 }}>
          {/* BP */}
          <div style={{ padding: 14, borderRadius: 12, border: `1px solid ${CC.border}`, background: '#fff' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 6 }}>
              <span style={{ fontSize: 14 }}>❤️</span>
              <span style={{ fontSize: 10, fontWeight: 700, color: CC.muted, letterSpacing: 0.4 }}>BLOOD PRESSURE</span>
            </div>
            <div style={{ fontSize: 22, fontWeight: 800, color: CC.text }}>{visit.bp_sys}/{visit.bp_dia}<span style={{ fontSize: 11, fontWeight: 500, color: CC.muted, marginLeft: 4 }}>mmHg</span></div>
            <div style={{ marginTop: 8, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 6 }}>
              <span style={{ fontSize: 10, fontWeight: 700, color: bp.color, background: bp.bg, padding: '2px 7px', borderRadius: 10 }}>{bp.label}</span>
              {bpSysΔ && bpSysΔ.dir !== 'flat' && (
                <span style={{ fontSize: 11, fontWeight: 700, color: trendColor(bpSysΔ) }}>
                  {trendArrow(bpSysΔ)} {bpSysΔ.abs}
                </span>
              )}
            </div>
          </div>

          {/* Sugar */}
          <div style={{ padding: 14, borderRadius: 12, border: `1px solid ${CC.border}`, background: '#fff' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 6 }}>
              <span style={{ fontSize: 14 }}>🩸</span>
              <span style={{ fontSize: 10, fontWeight: 700, color: CC.muted, letterSpacing: 0.4 }}>BLOOD SUGAR</span>
            </div>
            <div style={{ fontSize: 22, fontWeight: 800, color: CC.text }}>{visit.sugar || '—'}<span style={{ fontSize: 11, fontWeight: 500, color: CC.muted, marginLeft: 4 }}>mg/dL</span></div>
            <div style={{ marginTop: 8, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 6 }}>
              {sg && <span style={{ fontSize: 10, fontWeight: 700, color: sg.color, background: sg.bg, padding: '2px 7px', borderRadius: 10 }}>{sg.label}</span>}
              {!sg && <span style={{ fontSize: 10, color: CC.muted }}>—</span>}
              {sugarΔ && sugarΔ.dir !== 'flat' && (
                <span style={{ fontSize: 11, fontWeight: 700, color: trendColor(sugarΔ) }}>
                  {trendArrow(sugarΔ)} {sugarΔ.abs}
                </span>
              )}
            </div>
          </div>

          {/* Weight */}
          <div style={{ padding: 14, borderRadius: 12, border: `1px solid ${CC.border}`, background: '#fff' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 6 }}>
              <span style={{ fontSize: 14 }}>⚖️</span>
              <span style={{ fontSize: 10, fontWeight: 700, color: CC.muted, letterSpacing: 0.4 }}>WEIGHT</span>
            </div>
            <div style={{ fontSize: 22, fontWeight: 800, color: CC.text }}>{visit.weight}<span style={{ fontSize: 11, fontWeight: 500, color: CC.muted, marginLeft: 4 }}>kg</span></div>
            <div style={{ marginTop: 8, display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 6 }}>
              {weightΔ && weightΔ.dir !== 'flat' && (
                <span style={{ fontSize: 11, fontWeight: 700, color: trendColor(weightΔ) }}>
                  {trendArrow(weightΔ)} {weightΔ.abs} kg
                </span>
              )}
              {(!weightΔ || weightΔ.dir === 'flat') && <span style={{ fontSize: 10, color: CC.muted }}>—</span>}
            </div>
          </div>

          {/* Days since prev */}
          <div style={{ padding: 14, borderRadius: 12, border: `1px solid ${CC.border}`, background: '#fff' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 6 }}>
              <span style={{ fontSize: 14 }}>📆</span>
              <span style={{ fontSize: 10, fontWeight: 700, color: CC.muted, letterSpacing: 0.4 }}>INTERVAL</span>
            </div>
            <div style={{ fontSize: 22, fontWeight: 800, color: CC.text }}>
              {prev ? Math.round((new Date(visit.date) - new Date(prev.date)) / (1000 * 60 * 60 * 24)) : '—'}
              <span style={{ fontSize: 11, fontWeight: 500, color: CC.muted, marginLeft: 4 }}>days</span>
            </div>
            <div style={{ marginTop: 8, fontSize: 10, color: CC.muted }}>
              {prev ? `since ${prev.date}` : 'First recorded visit'}
            </div>
          </div>
        </div>

        {/* Comparison strip vs previous visit */}
        {prev && (
          <div style={{ padding: '12px 14px', background: '#F0F7FF', borderRadius: 10, marginBottom: 18, border: `1px solid ${CC.sky}` }}>
            <div style={{ fontSize: 10, fontWeight: 700, color: CC.primary, letterSpacing: 0.4, marginBottom: 6 }}>↻ COMPARED TO PREVIOUS VISIT ({prev.date})</div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 10, fontSize: 11 }}>
              <div><span style={{ color: CC.muted }}>BP:</span> <strong>{prev.bp_sys}/{prev.bp_dia}</strong> → <strong style={{ color: CC.text }}>{visit.bp_sys}/{visit.bp_dia}</strong></div>
              <div><span style={{ color: CC.muted }}>Sugar:</span> <strong>{prev.sugar || '—'}</strong> → <strong style={{ color: CC.text }}>{visit.sugar || '—'}</strong></div>
              <div><span style={{ color: CC.muted }}>Weight:</span> <strong>{prev.weight}kg</strong> → <strong style={{ color: CC.text }}>{visit.weight}kg</strong></div>
              <div><span style={{ color: CC.muted }}>Dx:</span> <strong style={{ color: CC.text }}>{prev.dx}</strong></div>
            </div>
          </div>
        )}

        {/* Medicines on this visit — with diff vs previous */}
        {meds.length > 0 && (
          <div style={{ marginBottom: 16 }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span style={{ fontSize: 11, fontWeight: 700, color: CC.primary, letterSpacing: 0.4 }}>℞ MEDICINES PRESCRIBED</span>
                <span style={{ fontSize: 10, fontWeight: 700, color: CC.muted, background: '#F1F5F9', padding: '2px 8px', borderRadius: 10 }}>{meds.length} drug{meds.length !== 1 ? 's' : ''}</span>
              </div>
              {relatedRx && <span style={{ fontSize: 10, fontWeight: 700, color: CC.muted, fontFamily: 'monospace' }}>{relatedRx.id}</span>}
            </div>
            <div style={{ borderRadius: 12, border: `1.5px solid ${CC.primary}`, background: CC.sky, overflow: 'hidden' }}>
              {meds.map((m, i) => {
                const st = medStatus(m);
                return (
                  <div key={i} style={{ padding: '10px 14px', display: 'flex', gap: 10, alignItems: 'flex-start', borderBottom: i < meds.length - 1 ? `1px solid rgba(21,101,192,0.12)` : 'none', background: st?.kind === 'new' ? 'rgba(16,185,129,0.06)' : st?.kind === 'changed' ? 'rgba(245,158,11,0.06)' : 'transparent' }}>
                    <div style={{ width: 22, height: 22, borderRadius: 6, background: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 11, fontWeight: 700, color: CC.primary, flexShrink: 0, marginTop: 1 }}>{i + 1}</div>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                        <span style={{ fontSize: 13, fontWeight: 700, color: CC.text }}>{m.drug} {m.strength}</span>
                        {st && <span style={{ fontSize: 9, fontWeight: 800, color: st.color, background: st.bg, padding: '2px 7px', borderRadius: 10, letterSpacing: 0.4 }}>{st.label}</span>}
                      </div>
                      <div style={{ fontSize: 11, color: CC.muted, marginTop: 2 }}>
                        {m.freq} · {m.duration}{typeof m.duration === 'number' ? ' days' : ''} · {m.timing}{m.instructions ? ` · ${Array.isArray(m.instructions) ? m.instructions.join(', ') : m.instructions}` : ''}
                      </div>
                      {st?.kind === 'changed' && (
                        <div style={{ fontSize: 10, color: CC.warning, marginTop: 4, fontWeight: 600 }}>
                          ↻ Changed from <s style={{ color: CC.muted }}>{st.from}</s> → <strong>{st.to}</strong>
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Discontinued meds — listed below */}
            {stoppedMeds.length > 0 && (
              <div style={{ marginTop: 8, padding: '10px 14px', borderRadius: 10, background: '#FEF2F2', border: `1px solid ${CC.error}` }}>
                <div style={{ fontSize: 10, fontWeight: 700, color: CC.error, letterSpacing: 0.4, marginBottom: 6 }}>✕ DISCONTINUED SINCE PREVIOUS VISIT</div>
                {stoppedMeds.map((m, i) => (
                  <div key={i} style={{ fontSize: 12, color: CC.text, textDecoration: 'line-through', textDecorationColor: CC.error }}>
                    {m.drug} {m.strength} · {m.freq}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {meds.length === 0 && (
          <div style={{ padding: '14px 16px', background: '#F8FAFC', borderRadius: 10, marginBottom: 16, fontSize: 12, color: CC.muted, textAlign: 'center' }}>
            No medicines recorded for this visit.
          </div>
        )}

        {/* Investigations + Notes */}
        {(investigations.length > 0 || notes) && (
          <div style={{ display: 'grid', gridTemplateColumns: investigations.length && notes ? '1fr 1fr' : '1fr', gap: 10, marginBottom: 16 }}>
            {investigations.length > 0 && (
              <div style={{ padding: '10px 14px', background: '#F5F3FF', borderRadius: 10, border: `1px solid #DDD6FE` }}>
                <div style={{ fontSize: 10, fontWeight: 700, color: '#7C3AED', letterSpacing: 0.4, marginBottom: 6 }}>🧪 INVESTIGATIONS ORDERED</div>
                <div style={{ fontSize: 12, color: CC.text, lineHeight: 1.5 }}>{investigations.join(' · ')}</div>
              </div>
            )}
            {notes && (
              <div style={{ padding: '10px 14px', background: '#FFFBEB', borderRadius: 10, border: `1px solid #FDE68A` }}>
                <div style={{ fontSize: 10, fontWeight: 700, color: '#92400E', letterSpacing: 0.4, marginBottom: 6 }}>📝 CLINICAL NOTES</div>
                <div style={{ fontSize: 12, color: CC.text, lineHeight: 1.5, fontStyle: 'italic' }}>{notes}</div>
              </div>
            )}
          </div>
        )}

        {/* Footer actions */}
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, paddingTop: 14, borderTop: `1px solid ${CC.border}` }}>
          <Btn variant="ghost" onClick={onClose}>Close</Btn>
          <Btn variant="ghost" onClick={handleOpenNewTab}>↗ Open in new tab</Btn>
        </div>
      </div>
    </Modal>
  );
}

// ─── Standalone visit HTML for new tab ─────────────────────────────────────
function buildStandaloneVisitHTML({ visit, idx, visits, patient, relatedRx, formattedDate, visitNumber, bp, sg, prev, bpSysΔ, bpDiaΔ, sugarΔ, weightΔ, meds, prevMeds, notes, investigations, stoppedMeds }) {
  const arrow = (d) => d?.dir === 'up' ? '↑' : d?.dir === 'down' ? '↓' : '→';
  const tColor = (d, lowerIsBetter = true) => {
    if (!d || d.dir === 'flat') return '#64748B';
    if (lowerIsBetter) return d.dir === 'down' ? '#10B981' : '#EF4444';
    return d.dir === 'up' ? '#10B981' : '#EF4444';
  };
  const days = prev ? Math.round((new Date(visit.date) - new Date(prev.date)) / (1000 * 60 * 60 * 24)) : null;

  const medStatusFor = (m) => {
    if (!prev) return null;
    const prior = (prevMeds || []).find(p => p.drug.toLowerCase() === m.drug.toLowerCase());
    if (!prior) return { kind: 'new', label: 'NEW', color: '#10B981', bg: '#ECFDF5' };
    if (prior.strength !== m.strength || prior.freq !== m.freq) {
      return { kind: 'changed', label: 'CHANGED', color: '#F59E0B', bg: '#FFFBEB', from: `${prior.strength} · ${prior.freq}`, to: `${m.strength} · ${m.freq}` };
    }
    return { kind: 'continued', label: 'CONTINUED', color: '#64748B', bg: '#F1F5F9' };
  };

  const medsHTML = (meds || []).map((m, i) => {
    const st = medStatusFor(m);
    const rowBg = st?.kind === 'new' ? 'rgba(16,185,129,0.06)' : st?.kind === 'changed' ? 'rgba(245,158,11,0.06)' : 'transparent';
    return `
    <div class="med" style="background:${rowBg};">
      <span class="med-i">${i + 1}</span>
      <div style="flex:1;">
        <div class="med-name-row">
          <strong>${m.drug}</strong> ${m.strength}
          ${st ? `<span class="med-badge" style="color:${st.color};background:${st.bg};">${st.label}</span>` : ''}
        </div>
        <div class="med-meta">${m.freq} · ${m.duration}${typeof m.duration === 'number' ? ' days' : ''} · ${m.timing}${m.instructions ? ` · ${Array.isArray(m.instructions) ? m.instructions.join(', ') : m.instructions}` : ''}</div>
        ${st?.kind === 'changed' ? `<div class="med-change">↻ Changed from <s style="color:#94A3B8;">${st.from}</s> → <strong>${st.to}</strong></div>` : ''}
      </div>
    </div>`;
  }).join('');

  const stoppedHTML = (stoppedMeds || []).length > 0 ? `
    <div class="stopped">
      <div class="stopped-label">✕ DISCONTINUED SINCE PREVIOUS VISIT</div>
      ${stoppedMeds.map(m => `<div class="stopped-row">${m.drug} ${m.strength} · ${m.freq}</div>`).join('')}
    </div>` : '';

  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<title>Visit #${visitNumber} · ${patient.name} · ${visit.date} – CloudClinic</title>
<link rel="preconnect" href="https://fonts.googleapis.com" />
<link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet" />
<style>
  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: 'Plus Jakarta Sans', sans-serif; background: #F4F6FA; color: #1E293B; padding: 32px 24px; -webkit-font-smoothing: antialiased; }
  .sheet { max-width: 880px; margin: 0 auto; background: #fff; border-radius: 16px; box-shadow: 0 4px 24px rgba(0,0,0,0.08); overflow: hidden; }
  .hero { padding: 28px 32px; background: linear-gradient(135deg, #0A1628 0%, #1565C0 100%); color: #fff; }
  .hero-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; }
  .brand { display: flex; align-items: center; gap: 10px; font-weight: 800; font-size: 14px; letter-spacing: 0.3px; }
  .brand-mark { width: 28px; height: 28px; border-radius: 8px; background: rgba(255,255,255,0.2); display: flex; align-items: center; justify-content: center; }
  .pill { display: inline-block; padding: 3px 10px; border-radius: 20px; font-size: 11px; font-weight: 700; background: rgba(255,255,255,0.18); letter-spacing: 0.4px; }
  .hero h1 { font-size: 28px; font-weight: 800; line-height: 1.15; margin-top: 6px; }
  .hero .date { font-size: 13px; opacity: 0.8; margin-top: 6px; }
  .patient-row { margin-top: 20px; padding-top: 18px; border-top: 1px solid rgba(255,255,255,0.15); display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 16px; }
  .field-label { font-size: 10px; opacity: 0.7; font-weight: 700; letter-spacing: 0.5px; text-transform: uppercase; margin-bottom: 3px; }
  .field-value { font-size: 13px; font-weight: 600; }
  .body { padding: 28px 32px; }
  .section-label { font-size: 11px; font-weight: 700; color: #64748B; letter-spacing: 0.5px; text-transform: uppercase; margin-bottom: 10px; }
  .vitals { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-bottom: 24px; }
  .vital { padding: 16px; border-radius: 12px; border: 1px solid #E2E8F0; }
  .vital-label { display: flex; align-items: center; gap: 6px; font-size: 10px; font-weight: 700; color: #64748B; letter-spacing: 0.4px; margin-bottom: 8px; }
  .vital-value { font-size: 26px; font-weight: 800; color: #0F172A; line-height: 1; }
  .vital-unit { font-size: 11px; font-weight: 500; color: #64748B; margin-left: 4px; }
  .vital-foot { margin-top: 10px; display: flex; justify-content: space-between; align-items: center; gap: 6px; }
  .tag { font-size: 10px; font-weight: 700; padding: 3px 8px; border-radius: 10px; }
  .trend { font-size: 12px; font-weight: 700; }
  .compare { padding: 14px 16px; background: #F0F7FF; border: 1px solid #DBEAFE; border-radius: 12px; margin-bottom: 24px; }
  .compare-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; font-size: 12px; }
  .compare-grid .lbl { color: #64748B; }
  .rx-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }
  .rx-title-row { display: flex; align-items: center; gap: 8px; }
  .rx-title { font-size: 11px; font-weight: 700; color: #1565C0; letter-spacing: 0.4px; }
  .rx-count { font-size: 10px; font-weight: 700; color: #64748B; background: #F1F5F9; padding: 2px 8px; border-radius: 10px; }
  .rx-id { font-size: 11px; font-weight: 700; color: #64748B; font-family: ui-monospace, Menlo, monospace; }
  .rx-card { border-radius: 12px; border: 1.5px solid #1565C0; background: #EFF6FF; overflow: hidden; margin-bottom: 16px; }
  .med { display: flex; gap: 10px; padding: 12px 14px; border-bottom: 1px solid rgba(21,101,192,0.12); align-items: flex-start; }
  .med:last-child { border-bottom: none; }
  .med-i { width: 22px; height: 22px; border-radius: 6px; background: #fff; color: #1565C0; font-size: 11px; font-weight: 700; display: flex; align-items: center; justify-content: center; flex-shrink: 0; margin-top: 1px; }
  .med-name-row { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; font-size: 13px; color: #0F172A; }
  .med-badge { font-size: 9px; font-weight: 800; padding: 2px 7px; border-radius: 10px; letter-spacing: 0.4px; }
  .med-meta { font-size: 11px; color: #64748B; margin-top: 3px; }
  .med-change { font-size: 10px; color: #F59E0B; margin-top: 4px; font-weight: 600; }
  .stopped { margin-top: 8px; padding: 10px 14px; border-radius: 10px; background: #FEF2F2; border: 1px solid #EF4444; }
  .stopped-label { font-size: 10px; font-weight: 700; color: #EF4444; letter-spacing: 0.4px; margin-bottom: 6px; }
  .stopped-row { font-size: 12px; color: #1E293B; text-decoration: line-through; text-decoration-color: #EF4444; }
  .info-grid { display: grid; gap: 10px; margin-bottom: 16px; }
  .info-card { padding: 10px 14px; border-radius: 10px; }
  .info-label { font-size: 10px; font-weight: 700; letter-spacing: 0.4px; margin-bottom: 6px; }
  .info-body { font-size: 12px; line-height: 1.5; color: #1E293B; }
  .info-investigations { background: #F5F3FF; border: 1px solid #DDD6FE; }
  .info-investigations .info-label { color: #7C3AED; }
  .info-notes { background: #FFFBEB; border: 1px solid #FDE68A; }
  .info-notes .info-label { color: #92400E; }
  .info-notes .info-body { font-style: italic; }
  .nope { padding: 16px; background: #F8FAFC; border-radius: 10px; text-align: center; color: #64748B; font-size: 12px; margin-bottom: 16px; }
  .foot { padding: 20px 32px; background: #F8FAFC; border-top: 1px solid #E2E8F0; font-size: 11px; color: #64748B; display: flex; justify-content: space-between; align-items: center; }
  .btn { padding: 8px 16px; border-radius: 8px; background: #1565C0; color: #fff; border: none; font-weight: 600; font-size: 12px; cursor: pointer; font-family: inherit; }
  @media print { body { background: #fff; padding: 0; } .sheet { box-shadow: none; max-width: none; border-radius: 0; } .foot { display: none; } }
</style>
</head>
<body>
  <div class="sheet">
    <div class="hero">
      <div class="hero-top">
        <div class="brand"><span class="brand-mark">☁️</span> CloudClinic</div>
        <div>
          <span class="pill">VISIT #${visitNumber} of ${visits.length}</span>
          ${idx === visits.length - 1 ? '<span class="pill" style="background:#10B981;margin-left:6px;">MOST RECENT</span>' : ''}
        </div>
      </div>
      <h1>${visit.dx}</h1>
      <div class="date">📅 ${formattedDate}</div>
      <div class="patient-row">
        <div>
          <div class="field-label">Patient</div>
          <div class="field-value">${patient.name}</div>
          <div style="font-size:11px;opacity:0.75;margin-top:2px;">${patient.uhid} · ${patient.age}y · ${patient.gender} · ${patient.blood}</div>
        </div>
        <div>
          <div class="field-label">Allergies</div>
          <div class="field-value">${patient.allergies}</div>
        </div>
        <div>
          <div class="field-label">Chronic Conditions</div>
          <div class="field-value">${patient.chronic.join(', ')}</div>
        </div>
      </div>
    </div>

    <div class="body">
      <div class="section-label">Vitals on this visit</div>
      <div class="vitals">
        <div class="vital">
          <div class="vital-label">❤️ BLOOD PRESSURE</div>
          <div><span class="vital-value">${visit.bp_sys}/${visit.bp_dia}</span><span class="vital-unit">mmHg</span></div>
          <div class="vital-foot">
            <span class="tag" style="background:${bp.bg};color:${bp.color};">${bp.label}</span>
            ${bpSysΔ && bpSysΔ.dir !== 'flat' ? `<span class="trend" style="color:${tColor(bpSysΔ)};">${arrow(bpSysΔ)} ${bpSysΔ.abs}</span>` : ''}
          </div>
        </div>
        <div class="vital">
          <div class="vital-label">🩸 BLOOD SUGAR</div>
          <div><span class="vital-value">${visit.sugar || '—'}</span>${visit.sugar ? '<span class="vital-unit">mg/dL</span>' : ''}</div>
          <div class="vital-foot">
            ${sg ? `<span class="tag" style="background:${sg.bg};color:${sg.color};">${sg.label}</span>` : '<span style="font-size:10px;color:#94A3B8;">—</span>'}
            ${sugarΔ && sugarΔ.dir !== 'flat' ? `<span class="trend" style="color:${tColor(sugarΔ)};">${arrow(sugarΔ)} ${sugarΔ.abs}</span>` : ''}
          </div>
        </div>
        <div class="vital">
          <div class="vital-label">⚖️ WEIGHT</div>
          <div><span class="vital-value">${visit.weight}</span><span class="vital-unit">kg</span></div>
          <div class="vital-foot">
            <span></span>
            ${weightΔ && weightΔ.dir !== 'flat' ? `<span class="trend" style="color:${tColor(weightΔ)};">${arrow(weightΔ)} ${weightΔ.abs} kg</span>` : '<span style="font-size:10px;color:#94A3B8;">—</span>'}
          </div>
        </div>
        <div class="vital">
          <div class="vital-label">📆 INTERVAL</div>
          <div><span class="vital-value">${days != null ? days : '—'}</span>${days != null ? '<span class="vital-unit">days</span>' : ''}</div>
          <div class="vital-foot"><span style="font-size:10px;color:#94A3B8;">${prev ? 'since ' + prev.date : 'First visit'}</span></div>
        </div>
      </div>

      ${prev ? `
      <div class="compare">
        <div class="section-label" style="color:#1565C0;margin-bottom:8px;">↻ Compared to previous visit (${prev.date})</div>
        <div class="compare-grid">
          <div><span class="lbl">BP:</span> <strong>${prev.bp_sys}/${prev.bp_dia}</strong> → <strong>${visit.bp_sys}/${visit.bp_dia}</strong></div>
          <div><span class="lbl">Sugar:</span> <strong>${prev.sugar || '—'}</strong> → <strong>${visit.sugar || '—'}</strong></div>
          <div><span class="lbl">Weight:</span> <strong>${prev.weight}kg</strong> → <strong>${visit.weight}kg</strong></div>
          <div><span class="lbl">Dx:</span> <strong>${prev.dx}</strong></div>
        </div>
      </div>` : ''}

      ${(meds || []).length > 0 ? `
        <div class="rx-head">
          <div class="rx-title-row">
            <span class="rx-title">℞ MEDICINES PRESCRIBED</span>
            <span class="rx-count">${meds.length} drug${meds.length !== 1 ? 's' : ''}</span>
          </div>
          ${relatedRx ? `<span class="rx-id">${relatedRx.id}</span>` : ''}
        </div>
        <div class="rx-card">${medsHTML}</div>
        ${stoppedHTML}
      ` : '<div class="nope">No medicines recorded for this visit.</div>'}

      ${(investigations?.length || notes) ? `
      <div class="info-grid" style="grid-template-columns:${investigations?.length && notes ? '1fr 1fr' : '1fr'};margin-top:16px;">
        ${investigations?.length ? `
          <div class="info-card info-investigations">
            <div class="info-label">🧪 INVESTIGATIONS ORDERED</div>
            <div class="info-body">${investigations.join(' · ')}</div>
          </div>` : ''}
        ${notes ? `
          <div class="info-card info-notes">
            <div class="info-label">📝 CLINICAL NOTES</div>
            <div class="info-body">${notes}</div>
          </div>` : ''}
      </div>` : ''}
    </div>

    <div class="foot">
      <div>Generated from CloudClinic visit history · ${new Date().toLocaleString('en-IN')}</div>
      <button class="btn" onclick="window.print()">🖨 Print</button>
    </div>
  </div>
</body>
</html>`;
}

Object.assign(window, { PrescriptionModule });
