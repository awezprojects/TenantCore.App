// CloudClinic – Sidebar Navigation
const { useState } = React;

const NAV_ITEMS = {
  doctor: [
    { id: 'dashboard', label: 'Dashboard', icon: '▦' },
    { id: 'opd', label: 'OPD', icon: '🩺' },
    { id: 'patients', label: 'Patients', icon: '👥' },
    { id: 'medicine', label: 'Medicines', icon: '💊' },
  ],
  receptionist: [
    { id: 'dashboard', label: 'Dashboard', icon: '▦' },
    { id: 'patients', label: 'Patient Reg.', icon: '👥' },
    { id: 'opd', label: 'OPD Queue', icon: '🩺' },
    { id: 'ipd', label: 'IPD / Beds', icon: '🛏️' },
  ],
  admin: [
    { id: 'dashboard', label: 'Dashboard', icon: '▦' },
    { id: 'patients', label: 'Patients', icon: '👥' },
    { id: 'opd', label: 'OPD', icon: '🩺' },
    { id: 'ipd', label: 'IPD / Beds', icon: '🛏️' },
    { id: 'admin-wards', label: 'Wards & Beds', icon: '🏥' },
    { id: 'medicine', label: 'Medicines', icon: '💊' },
    { id: 'prescription', label: 'Prescriptions', icon: '📋' },
  ],
  patient: [
    { id: 'dashboard', label: 'My Health', icon: '▦' },
    { id: 'prescription', label: 'Prescriptions', icon: '📋' },
  ],
};

const ROLE_META = {
  doctor:       { label: 'Dr. Arjun Mehta',    sub: 'Senior Physician · Cardiology',   avatar: '👨‍⚕️', badge: 'MD' },
  receptionist: { label: 'Priya Sharma',        sub: 'Reception · Front Desk',          avatar: '👩‍💼', badge: 'RX' },
  admin:        { label: 'Admin · CloudClinic', sub: 'Clinic Administrator',             avatar: '⚙️',  badge: 'AD' },
  patient:      { label: 'Ramesh Kumar',        sub: 'Patient · UHID-2024-0042',        avatar: '👤',  badge: 'PT' },
};

function Sidebar({ role, active, onNav, onLogout, clinic, onSwitchClinic }) {
  const items = NAV_ITEMS[role] || NAV_ITEMS.doctor;
  const meta  = ROLE_META[role]  || ROLE_META.doctor;
  const [collapsed, setCollapsed] = useState(false);

  return (
    <div style={{
      width: collapsed ? 68 : 230,
      minHeight: '100vh',
      background: CC.navy,
      display: 'flex', flexDirection: 'column',
      transition: 'width 0.25s cubic-bezier(.4,0,.2,1)',
      flexShrink: 0,
      position: 'relative',
      zIndex: 10,
    }}>
      {/* Logo */}
      <div style={{ padding: collapsed ? '20px 0' : '20px 20px', display: 'flex', alignItems: 'center', gap: 10, borderBottom: '1px solid rgba(255,255,255,0.08)', minHeight: 68 }}>
        <div style={{ width: 36, height: 36, borderRadius: 10, background: 'linear-gradient(135deg,#1976D2,#42A5F5)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 18, flexShrink: 0 }}>🏥</div>
        {!collapsed && <div>
          <div style={{ fontSize: 15, fontWeight: 800, color: '#fff', letterSpacing: -0.3 }}>CloudClinic</div>
          <div style={{ fontSize: 10, color: 'rgba(255,255,255,0.4)', letterSpacing: 0.5 }}>HMS v2.4</div>
        </div>}
      </div>

      {/* Active Clinic badge (for doctor) */}
      {clinic && !collapsed && (
        <div style={{ padding: '14px 16px 14px', borderBottom: '1px solid rgba(255,255,255,0.08)', background: 'rgba(255,255,255,0.03)' }}>
          <div style={{ fontSize: 9, color: 'rgba(255,255,255,0.45)', fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 6 }}>Current Clinic</div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{ width: 32, height: 32, borderRadius: 8, background: `linear-gradient(135deg,${clinic.color},${clinic.color}cc)`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 16, flexShrink: 0 }}>{clinic.logo}</div>
            <div style={{ overflow: 'hidden', flex: 1 }}>
              <div style={{ fontSize: 12, fontWeight: 700, color: '#fff', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{clinic.name}</div>
              <div style={{ fontSize: 10, color: 'rgba(255,255,255,0.5)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{clinic.role}</div>
            </div>
          </div>
          <button onClick={onSwitchClinic} style={{
            width: '100%', marginTop: 10, padding: '6px 8px', background: 'rgba(255,255,255,0.08)',
            color: 'rgba(255,255,255,0.8)', border: '1px solid rgba(255,255,255,0.1)', borderRadius: 6,
            cursor: 'pointer', fontSize: 11, fontWeight: 600,
          }}
          onMouseEnter={e => e.currentTarget.style.background = 'rgba(255,255,255,0.15)'}
          onMouseLeave={e => e.currentTarget.style.background = 'rgba(255,255,255,0.08)'}
          >🔄 Switch Clinic</button>
        </div>
      )}

      {/* Role badge */}
      {!collapsed && !clinic && (
        <div style={{ padding: '16px 16px 12px', borderBottom: '1px solid rgba(255,255,255,0.08)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{ width: 38, height: 38, borderRadius: 10, background: 'rgba(255,255,255,0.1)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 20, flexShrink: 0 }}>{meta.avatar}</div>
            <div style={{ overflow: 'hidden' }}>
              <div style={{ fontSize: 13, fontWeight: 700, color: '#fff', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{meta.label}</div>
              <div style={{ fontSize: 10, color: 'rgba(255,255,255,0.45)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{meta.sub}</div>
            </div>
          </div>
        </div>
      )}

      {/* Nav items */}
      <nav style={{ flex: 1, padding: '12px 8px', display: 'flex', flexDirection: 'column', gap: 2 }}>
        {items.map(item => {
          const isActive = active === item.id;
          return (
            <button key={item.id} onClick={() => onNav(item.id)} title={collapsed ? item.label : ''} style={{
              display: 'flex', alignItems: 'center', gap: 12,
              padding: collapsed ? '11px 0' : '11px 14px',
              justifyContent: collapsed ? 'center' : 'flex-start',
              borderRadius: 10, border: 'none', cursor: 'pointer',
              background: isActive ? 'rgba(255,255,255,0.12)' : 'transparent',
              color: isActive ? '#fff' : 'rgba(255,255,255,0.55)',
              fontWeight: isActive ? 700 : 500, fontSize: 14,
              transition: 'all 0.15s',
              position: 'relative',
            }}
            onMouseEnter={e => { if (!isActive) e.currentTarget.style.background = 'rgba(255,255,255,0.06)'; }}
            onMouseLeave={e => { if (!isActive) e.currentTarget.style.background = 'transparent'; }}
            >
              {isActive && <div style={{ position: 'absolute', left: 0, top: '20%', height: '60%', width: 3, background: CC.light, borderRadius: 2 }} />}
              <span style={{ fontSize: 16 }}>{item.icon}</span>
              {!collapsed && <span>{item.label}</span>}
            </button>
          );
        })}
      </nav>

      {/* Bottom */}
      <div style={{ padding: '12px 8px', borderTop: '1px solid rgba(255,255,255,0.08)', display: 'flex', flexDirection: 'column', gap: 2 }}>
        <button onClick={() => setCollapsed(c => !c)} style={{
          display: 'flex', alignItems: 'center', justifyContent: collapsed ? 'center' : 'flex-start',
          gap: 10, padding: collapsed ? '10px 0' : '10px 14px', borderRadius: 10, border: 'none',
          background: 'transparent', color: 'rgba(255,255,255,0.4)', cursor: 'pointer', fontSize: 14, transition: 'all 0.15s',
        }}
        onMouseEnter={e => e.currentTarget.style.color = '#fff'}
        onMouseLeave={e => e.currentTarget.style.color = 'rgba(255,255,255,0.4)'}
        >
          <span style={{ fontSize: 16 }}>{collapsed ? '→' : '←'}</span>
          {!collapsed && <span>Collapse</span>}
        </button>
        <button onClick={onLogout} style={{
          display: 'flex', alignItems: 'center', justifyContent: collapsed ? 'center' : 'flex-start',
          gap: 10, padding: collapsed ? '10px 0' : '10px 14px', borderRadius: 10, border: 'none',
          background: 'transparent', color: 'rgba(255,255,255,0.4)', cursor: 'pointer', fontSize: 14, transition: 'all 0.15s',
        }}
        onMouseEnter={e => e.currentTarget.style.color = '#FF6B6B'}
        onMouseLeave={e => e.currentTarget.style.color = 'rgba(255,255,255,0.4)'}
        >
          <span style={{ fontSize: 16 }}>🚪</span>
          {!collapsed && <span>Logout</span>}
        </button>
      </div>
    </div>
  );
}

// TopBar
function TopBar({ role, screen, onNotif, clinic, onSwitchClinic }) {
  const [time] = useState(new Date().toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' }));
  const titles = {
    dashboard: 'Dashboard', patients: 'Patient Management', opd: 'OPD Management',
    ipd: 'IPD & Bed Management', 'admin-wards': 'Wards, Rooms & Beds', prescription: 'Prescription Writer', medicine: 'Medicine Management',
    appointments: "Today's Appointments", 'prescriptions-list': 'Prescriptions',
  };
  return (
    <div style={{
      height: 60, background: '#fff', borderBottom: `1px solid ${CC.border}`,
      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      padding: '0 24px', flexShrink: 0,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
        {clinic && (
          <button onClick={onSwitchClinic} style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 12px', background: `${clinic.color}10`, border: `1px solid ${clinic.color}30`, borderRadius: 10, cursor: 'pointer', transition: 'all 0.15s' }}
            onMouseEnter={e => e.currentTarget.style.background = `${clinic.color}20`}
            onMouseLeave={e => e.currentTarget.style.background = `${clinic.color}10`}
          >
            <span style={{ fontSize: 14 }}>{clinic.logo}</span>
            <span style={{ fontSize: 12, fontWeight: 700, color: clinic.color }}>{clinic.name}</span>
            <span style={{ fontSize: 10, color: CC.muted }}>↻</span>
          </button>
        )}
        <div>
          <div style={{ fontSize: 17, fontWeight: 700, color: CC.text }}>{titles[screen] || 'Dashboard'}</div>
          <div style={{ fontSize: 11, color: CC.muted }}>
            {new Date().toLocaleDateString('en-IN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })} · {time}
          </div>
        </div>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <div style={{ padding: '6px 14px', background: CC.sky, borderRadius: 20, fontSize: 12, fontWeight: 600, color: CC.primary }}>
          🟢 Online
        </div>
        <button onClick={onNotif} style={{ width: 36, height: 36, borderRadius: 10, border: `1px solid ${CC.border}`, background: '#fff', cursor: 'pointer', fontSize: 16, position: 'relative' }}>
          🔔
          <span style={{ position: 'absolute', top: 6, right: 6, width: 8, height: 8, borderRadius: '50%', background: CC.error, border: '1.5px solid #fff' }} />
        </button>
      </div>
    </div>
  );
}

// Stat Card
function StatCard({ label, value, sub, color, icon, trend }) {
  return (
    <div style={{
      background: color || '#fff', borderRadius: 14, padding: '20px 20px',
      boxShadow: '0 2px 12px rgba(0,0,0,0.06)', flex: 1, minWidth: 160,
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <div>
          <div style={{ fontSize: 12, fontWeight: 600, color: 'rgba(0,0,0,0.45)', marginBottom: 6, textTransform: 'uppercase', letterSpacing: 0.5 }}>{label}</div>
          <div style={{ fontSize: 28, fontWeight: 800, color: CC.text }}>{value}</div>
          {sub && <div style={{ fontSize: 12, color: CC.muted, marginTop: 4 }}>{sub}</div>}
        </div>
        <div style={{ fontSize: 28, opacity: 0.8 }}>{icon}</div>
      </div>
      {trend !== undefined && (
        <div style={{ marginTop: 12, fontSize: 12, fontWeight: 600, color: trend >= 0 ? CC.success : CC.error }}>
          {trend >= 0 ? '↑' : '↓'} {Math.abs(trend)}% vs yesterday
        </div>
      )}
    </div>
  );
}

// Badge
function Badge({ children, color = CC.primary, bg = CC.sky }) {
  return (
    <span style={{ display: 'inline-block', padding: '3px 10px', borderRadius: 20, fontSize: 11, fontWeight: 700, color, background: bg }}>
      {children}
    </span>
  );
}

// Table helpers
function Table({ cols, rows, onRow }) {
  return (
    <div style={{ overflowX: 'auto' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
        <thead>
          <tr style={{ background: '#F8FAFC' }}>
            {cols.map(c => (
              <th key={c} style={{ padding: '11px 14px', textAlign: 'left', fontWeight: 700, color: CC.muted, fontSize: 11, textTransform: 'uppercase', letterSpacing: 0.5, borderBottom: `1px solid ${CC.border}` }}>{c}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={i} onClick={() => onRow && onRow(row)} style={{ borderBottom: `1px solid ${CC.border}`, cursor: onRow ? 'pointer' : 'default', transition: 'background 0.12s' }}
              onMouseEnter={e => e.currentTarget.style.background = '#F8FAFC'}
              onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
            >
              {row.cells.map((cell, j) => (
                <td key={j} style={{ padding: '12px 14px', color: CC.text, verticalAlign: 'middle' }}>{cell}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// Card wrapper
function Card({ children, style: s = {}, title, action }) {
  return (
    <div style={{ background: '#fff', borderRadius: 14, boxShadow: '0 2px 12px rgba(0,0,0,0.06)', overflow: 'hidden', ...s }}>
      {title && (
        <div style={{ padding: '16px 20px', borderBottom: `1px solid ${CC.border}`, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div style={{ fontWeight: 700, fontSize: 15, color: CC.text }}>{title}</div>
          {action}
        </div>
      )}
      {children}
    </div>
  );
}

// Btn
function Btn({ children, onClick, variant = 'primary', size = 'md', style: s = {} }) {
  const sizes = { sm: { padding: '6px 14px', fontSize: 12 }, md: { padding: '9px 18px', fontSize: 13 }, lg: { padding: '12px 24px', fontSize: 14 } };
  const variants = {
    primary: { background: `linear-gradient(135deg,${CC.primary},${CC.mid})`, color: '#fff', border: 'none', boxShadow: '0 3px 10px rgba(21,101,192,0.3)' },
    ghost: { background: 'transparent', color: CC.primary, border: `1.5px solid ${CC.primary}` },
    danger: { background: CC.error, color: '#fff', border: 'none' },
    success: { background: CC.success, color: '#fff', border: 'none' },
    light: { background: CC.sky, color: CC.primary, border: 'none' },
  };
  return (
    <button onClick={onClick} style={{ ...sizes[size], ...variants[variant], borderRadius: 8, fontWeight: 600, cursor: 'pointer', transition: 'all 0.15s', fontFamily: 'inherit', ...s }}>
      {children}
    </button>
  );
}

// Modal
function Modal({ open, onClose, title, children, width = 600 }) {
  if (!open) return null;
  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(10,22,40,0.55)', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 20 }} onClick={onClose}>
      <div className="fade-in" style={{ background: '#fff', borderRadius: 16, width: '100%', maxWidth: width, maxHeight: '90vh', overflow: 'auto', boxShadow: '0 20px 60px rgba(0,0,0,0.25)' }} onClick={e => e.stopPropagation()}>
        <div style={{ padding: '20px 24px', borderBottom: `1px solid ${CC.border}`, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div style={{ fontWeight: 700, fontSize: 16 }}>{title}</div>
          <button onClick={onClose} style={{ background: 'none', border: 'none', fontSize: 20, cursor: 'pointer', color: CC.muted }}>✕</button>
        </div>
        <div style={{ padding: 24 }}>{children}</div>
      </div>
    </div>
  );
}

// FormField
function FormField({ label, children, required }) {
  return (
    <div style={{ marginBottom: 16 }}>
      <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: CC.muted, marginBottom: 5, textTransform: 'uppercase', letterSpacing: 0.4 }}>
        {label}{required && <span style={{ color: CC.error }}> *</span>}
      </label>
      {children}
    </div>
  );
}

function Input({ value, onChange, placeholder, type = 'text', disabled }) {
  return (
    <input type={type} value={value} onChange={onChange} placeholder={placeholder} disabled={disabled} style={{
      width: '100%', padding: '9px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13,
      color: CC.text, background: disabled ? '#F8FAFC' : '#fff', outline: 'none', transition: 'border-color 0.2s',
    }}
    onFocus={e => { if (!disabled) e.target.style.borderColor = CC.primary; }}
    onBlur={e => e.target.style.borderColor = CC.border}
    />
  );
}

function Select({ value, onChange, options }) {
  return (
    <select value={value} onChange={onChange} style={{
      width: '100%', padding: '9px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13,
      color: CC.text, background: '#fff', outline: 'none', cursor: 'pointer',
    }}>
      {options.map(o => <option key={o.value || o} value={o.value || o}>{o.label || o}</option>)}
    </select>
  );
}

Object.assign(window, { Sidebar, TopBar, StatCard, Badge, Table, Card, Btn, Modal, FormField, Input, Select, NAV_ITEMS, ROLE_META });
