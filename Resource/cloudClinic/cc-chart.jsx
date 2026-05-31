// CloudClinic – BP / Sugar Chart Component (SVG line chart)
const { useState: _us, useMemo: _um } = React;

function VitalsChart({ visits, metric }) {
  // metric: 'bp' | 'sugar'
  if (!visits || visits.length === 0) return null;

  const W = 540, H = 180, P = { l: 36, r: 16, t: 18, b: 28 };
  const innerW = W - P.l - P.r;
  const innerH = H - P.t - P.b;

  const config = metric === 'bp'
    ? { title: 'Blood Pressure Trend', unit: 'mmHg', yMin: 60, yMax: 180,
        series: [
          { key: 'bp_sys', label: 'Systolic',  color: CC.error,   target: 130 },
          { key: 'bp_dia', label: 'Diastolic', color: CC.primary, target: 85 },
        ] }
    : { title: 'Blood Sugar Trend (Fasting)', unit: 'mg/dL', yMin: 70, yMax: 220,
        series: [{ key: 'sugar', label: 'Glucose', color: '#7C3AED', target: 130 }] };

  const xs = visits.map((_, i) => P.l + (i * innerW) / Math.max(1, visits.length - 1));
  const yScale = v => P.t + innerH - ((v - config.yMin) / (config.yMax - config.yMin)) * innerH;

  // y-axis ticks
  const ticks = 4;
  const tickVals = Array.from({ length: ticks + 1 }, (_, i) => Math.round(config.yMin + (i * (config.yMax - config.yMin)) / ticks));

  // path generator
  const pathFor = (key) => visits.map((v, i) => `${i === 0 ? 'M' : 'L'} ${xs[i]} ${yScale(v[key])}`).join(' ');

  return (
    <div style={{ background: '#fff', borderRadius: 10, padding: 14, border: `1px solid ${CC.border}` }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 }}>
        <div>
          <div style={{ fontWeight: 700, fontSize: 13, color: CC.text }}>{config.title}</div>
          <div style={{ fontSize: 10, color: CC.muted }}>{visits.length} visits · {visits[0].date} → {visits[visits.length-1].date}</div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          {config.series.map(s => (
            <div key={s.key} style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 10, color: CC.muted, fontWeight: 600 }}>
              <span style={{ width: 8, height: 8, borderRadius: '50%', background: s.color }}></span>{s.label}
            </div>
          ))}
        </div>
      </div>
      <svg viewBox={`0 0 ${W} ${H}`} style={{ width: '100%', height: 180, overflow: 'visible' }}>
        {/* Grid */}
        {tickVals.map((v, i) => {
          const y = yScale(v);
          return (
            <g key={i}>
              <line x1={P.l} y1={y} x2={W - P.r} y2={y} stroke="#E5E7EB" strokeDasharray="2 3" />
              <text x={P.l - 8} y={y + 3} fontSize="9" fill={CC.muted} textAnchor="end">{v}</text>
            </g>
          );
        })}
        {/* Target line */}
        {config.series.map((s, si) => (
          <g key={'t'+si}>
            <line x1={P.l} y1={yScale(s.target)} x2={W - P.r} y2={yScale(s.target)} stroke={s.color} strokeDasharray="3 3" strokeOpacity={0.35} />
          </g>
        ))}
        {/* X-axis labels */}
        {visits.map((v, i) => (
          <text key={i} x={xs[i]} y={H - 10} fontSize="9" fill={CC.muted} textAnchor="middle">
            {v.date.slice(5)}
          </text>
        ))}
        {/* Series */}
        {config.series.map((s, si) => (
          <g key={si}>
            <path d={pathFor(s.key)} fill="none" stroke={s.color} strokeWidth="2" />
            {visits.map((v, i) => (
              <g key={i}>
                <circle cx={xs[i]} cy={yScale(v[s.key])} r={3.5} fill="#fff" stroke={s.color} strokeWidth="2" />
                {(i === visits.length - 1) && (
                  <text x={xs[i]} y={yScale(v[s.key]) - 8} fontSize="10" fontWeight="700" fill={s.color} textAnchor="middle">{v[s.key]}</text>
                )}
              </g>
            ))}
          </g>
        ))}
      </svg>
    </div>
  );
}

Object.assign(window, { VitalsChart });
