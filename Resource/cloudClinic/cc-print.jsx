// CloudClinic – Printable Prescription View
const { useState: _u } = React;

function PrintPrescription({ rx, onBack }) {
  const handlePrint = () => window.print();

  return (
    <>
      <style>{`
        @media print {
          .no-print { display: none !important; }
          body { background: white !important; }
          .print-page { box-shadow: none !important; margin: 0 !important; padding: 28px !important; max-width: 100% !important; }
        }
        @page { size: A4; margin: 0; }
      `}</style>

      {/* Top toolbar */}
      <div className="no-print" style={{ padding: '16px 24px', background: '#fff', borderBottom: `1px solid ${CC.border}`, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <button onClick={onBack} style={{ background: 'none', border: 'none', color: CC.muted, cursor: 'pointer', fontSize: 14 }}>← Back</button>
        <div style={{ fontSize: 15, fontWeight: 700 }}>Print Preview · {rx.id}</div>
        <Btn onClick={handlePrint}>🖨️ Print Now</Btn>
      </div>

      <div style={{ background: '#E5E7EB', padding: '24px 0', minHeight: 'calc(100vh - 60px)' }}>
        {/* A4 sheet */}
        <div className="print-page" style={{
          width: 794, maxWidth: '100%', margin: '0 auto', background: '#fff',
          padding: 40, boxShadow: '0 4px 24px rgba(0,0,0,0.1)', minHeight: 1123,
          fontFamily: '"Plus Jakarta Sans", "Noto Sans Devanagari", sans-serif',
          color: '#1E293B',
        }}>
          {/* Header */}
          <div style={{ borderBottom: `3px solid ${CC.primary}`, paddingBottom: 16, marginBottom: 18, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <div style={{ display: 'flex', gap: 14, alignItems: 'flex-start' }}>
              <div style={{ width: 56, height: 56, borderRadius: 14, background: `linear-gradient(135deg,${CC.primary},${CC.mid})`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 28, color: '#fff' }}>🏥</div>
              <div>
                <div style={{ fontSize: 22, fontWeight: 800, color: CC.navy, letterSpacing: -0.5 }}>CloudClinic Hospital</div>
                <div style={{ fontSize: 11, color: CC.muted }}>123 Healthcare Avenue, Mumbai 400001 · Phone: +91 22 4567 8900</div>
                <div style={{ fontSize: 11, color: CC.muted }}>Reg. No: MH/HSP/2024/0892 · GSTIN: 27ABCDE1234F1Z5</div>
              </div>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div style={{ fontSize: 14, fontWeight: 700, color: CC.text }}>Dr. Arjun Mehta</div>
              <div style={{ fontSize: 10, color: CC.muted }}>MBBS, MD (Cardiology)</div>
              <div style={{ fontSize: 10, color: CC.muted }}>Reg. No: MCI-12345</div>
            </div>
          </div>

          {/* Patient info */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 0, marginBottom: 16, padding: '10px 0', borderBottom: `1px solid ${CC.border}` }}>
            <div>
              <div style={{ fontSize: 9, color: CC.muted, textTransform: 'uppercase', fontWeight: 600, letterSpacing: 0.4 }}>Patient Name</div>
              <div style={{ fontSize: 13, fontWeight: 700 }}>{rx.name}</div>
            </div>
            <div>
              <div style={{ fontSize: 9, color: CC.muted, textTransform: 'uppercase', fontWeight: 600, letterSpacing: 0.4 }}>Age / Sex</div>
              <div style={{ fontSize: 13, fontWeight: 700 }}>{rx.age} yrs / {rx.gender === 'M' ? 'Male' : 'Female'}</div>
            </div>
            <div>
              <div style={{ fontSize: 9, color: CC.muted, textTransform: 'uppercase', fontWeight: 600, letterSpacing: 0.4 }}>UHID</div>
              <div style={{ fontSize: 13, fontWeight: 700, color: CC.primary }}>{rx.uhid}</div>
            </div>
            <div>
              <div style={{ fontSize: 9, color: CC.muted, textTransform: 'uppercase', fontWeight: 600, letterSpacing: 0.4 }}>Date / Time</div>
              <div style={{ fontSize: 13, fontWeight: 700 }}>{rx.date} · {rx.time}</div>
            </div>
          </div>

          {/* Vitals */}
          <div style={{ marginBottom: 16 }}>
            <div style={{ fontSize: 11, fontWeight: 700, color: CC.primary, marginBottom: 6, textTransform: 'uppercase', letterSpacing: 0.5 }}>Vital Signs</div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7,1fr)', gap: 6, padding: '8px 0', borderTop: `1px solid ${CC.border}`, borderBottom: `1px solid ${CC.border}` }}>
              {Object.entries(rx.vitals).map(([k, v]) => (
                <div key={k} style={{ textAlign: 'center', padding: '4px 6px' }}>
                  <div style={{ fontSize: 9, color: CC.muted, textTransform: 'uppercase' }}>{k}</div>
                  <div style={{ fontSize: 12, fontWeight: 700 }}>{v}</div>
                </div>
              ))}
            </div>
          </div>

          {/* Diagnosis */}
          <div style={{ marginBottom: 16 }}>
            <div style={{ fontSize: 11, fontWeight: 700, color: CC.primary, marginBottom: 6, textTransform: 'uppercase', letterSpacing: 0.5 }}>Diagnosis</div>
            <div style={{ fontSize: 13, padding: '6px 0', fontWeight: 600 }}>{rx.diagnosis}</div>
          </div>

          {/* Medicines */}
          <div style={{ marginBottom: 16 }}>
            <div style={{ fontSize: 11, fontWeight: 700, color: CC.primary, marginBottom: 8, textTransform: 'uppercase', letterSpacing: 0.5 }}>
              ℞ Rx — Medicines Prescribed
            </div>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
              <thead>
                <tr style={{ borderBottom: `2px solid ${CC.text}` }}>
                  <th style={{ padding: '6px 4px', textAlign: 'left', fontWeight: 700, width: 24 }}>#</th>
                  <th style={{ padding: '6px 4px', textAlign: 'left', fontWeight: 700 }}>Medicine</th>
                  <th style={{ padding: '6px 4px', textAlign: 'center', fontWeight: 700, width: 70 }}>Frequency</th>
                  <th style={{ padding: '6px 4px', textAlign: 'center', fontWeight: 700, width: 80 }}>Duration</th>
                  <th style={{ padding: '6px 4px', textAlign: 'left', fontWeight: 700 }}>Instructions</th>
                </tr>
              </thead>
              <tbody>
                {rx.meds.map((m, i) => {
                  const remark = m.remark || generateRemark(m, rx.lang || 'en');
                  return (
                    <tr key={i} style={{ borderBottom: `1px solid ${CC.border}`, verticalAlign: 'top' }}>
                      <td style={{ padding: '8px 4px', fontWeight: 700 }}>{i+1}</td>
                      <td style={{ padding: '8px 4px' }}>
                        <div style={{ fontWeight: 700, fontSize: 13 }}>{m.drug} {m.strength}</div>
                        <div style={{ fontSize: 10, color: CC.muted }}>{m.form}</div>
                      </td>
                      <td style={{ padding: '8px 4px', textAlign: 'center', fontWeight: 600 }}>{m.freq}</td>
                      <td style={{ padding: '8px 4px', textAlign: 'center' }}>{typeof m.duration === 'number' ? `${m.duration} days` : m.duration}</td>
                      <td style={{ padding: '8px 4px', fontSize: 11, lineHeight: 1.5 }}>
                        <div style={{ fontStyle: 'italic' }}>{remark}</div>
                        {(() => {
                          const list = Array.isArray(m.instructions) ? m.instructions : (m.instructions ? [m.instructions] : []);
                          return list.length > 0 && list.map((inst, k) => (
                            <div key={k} style={{ marginTop: 2, color: CC.primary, fontSize: 10 }}>• {inst}</div>
                          ));
                        })()}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Investigations */}
          {rx.investigations && rx.investigations.length > 0 && (
            <div style={{ marginBottom: 16, padding: '10px 12px', background: '#F5F3FF', borderRadius: 6, border: '1px solid #DDD6FE' }}>
              <div style={{ fontSize: 11, fontWeight: 700, color: '#7C3AED', marginBottom: 6, textTransform: 'uppercase', letterSpacing: 0.5 }}>🔬 Investigations Advised</div>
              <ol style={{ paddingLeft: 18, margin: 0, fontSize: 12, lineHeight: 1.7 }}>
                {rx.investigations.map((inv, i) => <li key={i} style={{ fontWeight: 500 }}>{inv}</li>)}
              </ol>
            </div>
          )}

          {/* Notes */}
          {rx.notes && (
            <div style={{ marginBottom: 16 }}>
              <div style={{ fontSize: 11, fontWeight: 700, color: CC.primary, marginBottom: 6, textTransform: 'uppercase', letterSpacing: 0.5 }}>📝 General Advice</div>
              <div style={{ fontSize: 12, lineHeight: 1.6, padding: '4px 0' }}>{rx.notes}</div>
            </div>
          )}

          {/* Next visit & Signature */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20, marginTop: 30, paddingTop: 16, borderTop: `2px solid ${CC.border}` }}>
            <div style={{ padding: '12px 14px', background: CC.sky, borderRadius: 8, border: `1px solid ${CC.primary}` }}>
              <div style={{ fontSize: 10, color: CC.primary, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.4 }}>⏰ Next Visit</div>
              <div style={{ fontSize: 14, fontWeight: 800, color: CC.navy, marginTop: 2 }}>
                {new Date(rx.followup).toLocaleDateString('en-IN', { weekday: 'short', day: '2-digit', month: 'long', year: 'numeric' })}
              </div>
              <div style={{ fontSize: 10, color: CC.muted, marginTop: 2 }}>Please arrive 15 minutes prior with previous reports</div>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div style={{ height: 50, borderBottom: `1.5px solid ${CC.text}`, marginBottom: 4, display: 'flex', alignItems: 'flex-end', justifyContent: 'flex-end', paddingBottom: 4 }}>
                <span style={{ fontFamily: 'cursive', fontSize: 18, fontStyle: 'italic', color: CC.primary }}>Dr. A. Mehta</span>
              </div>
              <div style={{ fontSize: 11, fontWeight: 700 }}>Dr. Arjun Mehta</div>
              <div style={{ fontSize: 10, color: CC.muted }}>MBBS, MD · Cardiology</div>
              <div style={{ fontSize: 10, color: CC.muted }}>Signature & Stamp</div>
            </div>
          </div>

          {/* Footer */}
          <div style={{ marginTop: 28, paddingTop: 10, borderTop: `1px dashed ${CC.border}`, fontSize: 9, color: CC.muted, textAlign: 'center', lineHeight: 1.6 }}>
            This prescription is computer-generated and digitally signed. For emergencies, call +91 22 4567 8911 (24×7).<br/>
            ⚠️ Self-medication can be harmful · Take medicines only as advised · Complete the full course
          </div>
        </div>
      </div>
    </>
  );
}

Object.assign(window, { PrintPrescription });
