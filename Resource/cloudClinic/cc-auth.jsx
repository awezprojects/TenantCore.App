// CloudClinic – Auth Screens: Login, OTP, Forgot Password
const { useState, useRef, useEffect } = React;

const CC = {
  navy: '#0A1628',
  sidebar: '#0F2040',
  primary: '#1565C0',
  mid: '#1976D2',
  light: '#42A5F5',
  sky: '#E3F2FD',
  bg: '#F4F6FA',
  white: '#FFFFFF',
  border: '#E2E8F0',
  text: '#1E293B',
  muted: '#64748B',
  success: '#059669',
  warning: '#D97706',
  error: '#DC2626',
};

function AuthInput({ label, type = 'text', value, onChange, placeholder, icon, rightEl }) {
  const [show, setShow] = useState(false);
  return (
    <div style={{ marginBottom: 18 }}>
      <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: CC.text, marginBottom: 6 }}>{label}</label>
      <div style={{ position: 'relative' }}>
        {icon && (
          <span style={{ position: 'absolute', left: 14, top: '50%', transform: 'translateY(-50%)', color: CC.muted, fontSize: 16 }}>{icon}</span>
        )}
        <input
          type={type === 'password' ? (show ? 'text' : 'password') : type}
          value={value}
          onChange={onChange}
          placeholder={placeholder}
          style={{
            width: '100%', padding: '11px 42px 11px', paddingLeft: icon ? 40 : 14,
            border: `1.5px solid ${CC.border}`, borderRadius: 10, fontSize: 14,
            color: CC.text, background: '#fff', outline: 'none',
            transition: 'border-color 0.2s',
          }}
          onFocus={e => e.target.style.borderColor = CC.primary}
          onBlur={e => e.target.style.borderColor = CC.border}
        />
        {type === 'password' && (
          <button onClick={() => setShow(s => !s)} style={{ position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)', background: 'none', border: 'none', color: CC.muted, fontSize: 16, cursor: 'pointer' }}>
            {show ? '🙈' : '👁️'}
          </button>
        )}
        {rightEl && <div style={{ position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)' }}>{rightEl}</div>}
      </div>
    </div>
  );
}

function AuthBtn({ children, onClick, disabled, variant = 'primary', style: extraStyle = {} }) {
  const base = {
    width: '100%', padding: '12px', borderRadius: 10, border: 'none',
    fontSize: 15, fontWeight: 700, cursor: disabled ? 'not-allowed' : 'pointer',
    transition: 'all 0.2s', letterSpacing: 0.3,
  };
  const variants = {
    primary: { background: disabled ? '#90CAF9' : `linear-gradient(135deg, ${CC.primary}, ${CC.mid})`, color: '#fff', boxShadow: disabled ? 'none' : '0 4px 16px rgba(21,101,192,0.35)' },
    ghost: { background: 'transparent', color: CC.primary, border: `1.5px solid ${CC.primary}` },
  };
  return (
    <button onClick={onClick} disabled={disabled} style={{ ...base, ...variants[variant], ...extraStyle }}>
      {children}
    </button>
  );
}

// ─── Login Screen ───────────────────────────────────────────────────────────
function LoginScreen({ onLogin, onForgot }) {
  const [role, setRole] = useState('doctor');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const roles = [
    { id: 'doctor', label: 'Doctor', icon: '🩺' },
    { id: 'receptionist', label: 'Reception', icon: '🗂️' },
    { id: 'admin', label: 'Admin', icon: '⚙️' },
    { id: 'patient', label: 'Patient', icon: '👤' },
  ];

  const handleLogin = () => {
    if (!email || !password) { setError('Please fill all fields'); return; }
    setError('');
    setLoading(true);
    setTimeout(() => { setLoading(false); onLogin(role); }, 1200);
  };

  return (
    <div style={{ display: 'flex', height: '100vh', width: '100vw' }}>
      {/* Left Brand Panel */}
      <div style={{
        width: '45%', background: `linear-gradient(160deg, ${CC.navy} 0%, #0D2456 60%, #1565C0 100%)`,
        display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center',
        padding: 48, position: 'relative', overflow: 'hidden',
      }}>
        {/* Decorative circles */}
        {[...Array(5)].map((_, i) => (
          <div key={i} style={{
            position: 'absolute',
            width: [320, 220, 160, 100, 60][i], height: [320, 220, 160, 100, 60][i],
            borderRadius: '50%', border: `1px solid rgba(255,255,255,${[0.04,0.06,0.08,0.1,0.12][i]})`,
            top: ['10%', '20%', '30%', '40%', '50%'][i], left: ['60%', '65%', '70%', '75%', '80%'][i],
          }} />
        ))}
        <div style={{ position: 'relative', zIndex: 1, textAlign: 'center' }}>
          <div style={{ fontSize: 52, marginBottom: 16 }}>🏥</div>
          <div style={{ fontSize: 32, fontWeight: 800, color: '#fff', letterSpacing: -0.5 }}>CloudClinic</div>
          <div style={{ fontSize: 14, color: 'rgba(255,255,255,0.6)', marginTop: 8, marginBottom: 40 }}>Healthcare Management System</div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16, marginTop: 16 }}>
            {[
              { icon: '✅', text: 'Unified Patient Records' },
              { icon: '✅', text: 'OPD / IPD Management' },
              { icon: '✅', text: 'Smart Prescription Writer' },
              { icon: '✅', text: 'Real-time Bed Tracking' },
            ].map(f => (
              <div key={f.text} style={{ display: 'flex', alignItems: 'center', gap: 12, color: 'rgba(255,255,255,0.8)', fontSize: 14 }}>
                <span style={{ fontSize: 16 }}>{f.icon}</span>{f.text}
              </div>
            ))}
          </div>
        </div>
        <div style={{ position: 'absolute', bottom: 24, color: 'rgba(255,255,255,0.35)', fontSize: 12 }}>
          v2.4.1 · HIPAA Compliant · ISO 27001
        </div>
      </div>

      {/* Right Form Panel */}
      <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', background: CC.bg, padding: 32 }}>
        <div className="fade-in" style={{ width: '100%', maxWidth: 420 }}>
          <div style={{ marginBottom: 32 }}>
            <div style={{ fontSize: 24, fontWeight: 800, color: CC.text }}>Welcome back</div>
            <div style={{ fontSize: 14, color: CC.muted, marginTop: 4 }}>Sign in to your CloudClinic account</div>
          </div>

          {/* Role Selector */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 8, marginBottom: 24 }}>
            {roles.map(r => (
              <button key={r.id} onClick={() => setRole(r.id)} style={{
                padding: '10px 4px', borderRadius: 10, border: `2px solid ${role === r.id ? CC.primary : CC.border}`,
                background: role === r.id ? CC.sky : '#fff', cursor: 'pointer', textAlign: 'center',
                transition: 'all 0.18s',
              }}>
                <div style={{ fontSize: 18 }}>{r.icon}</div>
                <div style={{ fontSize: 11, fontWeight: 600, color: role === r.id ? CC.primary : CC.muted, marginTop: 3 }}>{r.label}</div>
              </button>
            ))}
          </div>

          <AuthInput label="Email Address" type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="name@hospital.com" icon="✉️" />
          <AuthInput label="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} placeholder="Enter password" icon="🔒" />

          {error && <div style={{ color: CC.error, fontSize: 13, marginBottom: 12, padding: '8px 12px', background: '#FEF2F2', borderRadius: 8 }}>{error}</div>}

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 20 }}>
            <button onClick={onForgot} style={{ background: 'none', border: 'none', color: CC.primary, fontSize: 13, fontWeight: 600, cursor: 'pointer' }}>Forgot password?</button>
          </div>

          <AuthBtn onClick={handleLogin} disabled={loading}>
            {loading ? 'Signing in...' : `Sign in as ${roles.find(r => r.id === role)?.label}`}
          </AuthBtn>

          <div style={{ textAlign: 'center', marginTop: 20, fontSize: 12, color: CC.muted }}>
            Protected by 2FA · All sessions encrypted
          </div>
        </div>
      </div>
    </div>
  );
}

// ─── OTP Screen ─────────────────────────────────────────────────────────────
function OTPScreen({ role, onVerify, onBack }) {
  const [otp, setOtp] = useState(['', '', '', '', '', '']);
  const [timer, setTimer] = useState(30);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const refs = useRef([]);

  useEffect(() => {
    if (timer > 0) { const t = setTimeout(() => setTimer(s => s - 1), 1000); return () => clearTimeout(t); }
  }, [timer]);

  const handleChange = (i, val) => {
    if (!/^\d*$/.test(val)) return;
    const next = [...otp]; next[i] = val.slice(-1);
    setOtp(next);
    if (val && i < 5) refs.current[i + 1]?.focus();
  };

  const handleKey = (i, e) => {
    if (e.key === 'Backspace' && !otp[i] && i > 0) refs.current[i - 1]?.focus();
  };

  const handleVerify = () => {
    if (otp.join('').length < 6) { setError('Enter 6-digit OTP'); return; }
    setError(''); setLoading(true);
    setTimeout(() => { setLoading(false); onVerify(); }, 1000);
  };

  const maskedEmail = role === 'doctor' ? 'dr.***@hospital.com' : 'user.***@hospital.com';

  return (
    <div style={{ display: 'flex', height: '100vh', width: '100vw' }}>
      <div style={{ width: '45%', background: `linear-gradient(160deg, ${CC.navy} 0%, #0D2456 60%, #1565C0 100%)`, display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: 48 }}>
        <div style={{ textAlign: 'center', color: '#fff' }}>
          <div style={{ fontSize: 56, marginBottom: 20 }}>🔐</div>
          <div style={{ fontSize: 24, fontWeight: 800, marginBottom: 10 }}>2-Factor Auth</div>
          <div style={{ fontSize: 14, color: 'rgba(255,255,255,0.6)', maxWidth: 260, lineHeight: 1.6 }}>
            An extra layer of security keeps patient data safe. This step is mandatory for all CloudClinic users.
          </div>
        </div>
      </div>
      <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', background: CC.bg, padding: 32 }}>
        <div className="fade-in" style={{ width: '100%', maxWidth: 400, textAlign: 'center' }}>
          <div style={{ fontSize: 22, fontWeight: 800, color: CC.text, marginBottom: 8 }}>Verify your identity</div>
          <div style={{ fontSize: 14, color: CC.muted, marginBottom: 8 }}>We sent a 6-digit code to</div>
          <div style={{ fontSize: 14, fontWeight: 700, color: CC.primary, marginBottom: 32 }}>{maskedEmail}</div>

          <div style={{ display: 'flex', gap: 10, justifyContent: 'center', marginBottom: 28 }}>
            {otp.map((digit, i) => (
              <input
                key={i}
                ref={el => refs.current[i] = el}
                value={digit}
                onChange={e => handleChange(i, e.target.value)}
                onKeyDown={e => handleKey(i, e)}
                maxLength={1}
                style={{
                  width: 50, height: 58, textAlign: 'center', fontSize: 22, fontWeight: 700,
                  border: `2px solid ${digit ? CC.primary : CC.border}`, borderRadius: 12,
                  color: CC.text, background: digit ? CC.sky : '#fff', outline: 'none',
                  transition: 'all 0.15s',
                }}
              />
            ))}
          </div>

          {error && <div style={{ color: CC.error, fontSize: 13, marginBottom: 12 }}>{error}</div>}

          <AuthBtn onClick={handleVerify} disabled={loading} style={{ marginBottom: 16 }}>
            {loading ? 'Verifying...' : 'Verify & Continue'}
          </AuthBtn>

          <div style={{ fontSize: 13, color: CC.muted }}>
            {timer > 0 ? (
              <span>Resend code in <strong style={{ color: CC.primary }}>0:{String(timer).padStart(2, '0')}</strong></span>
            ) : (
              <button onClick={() => setTimer(30)} style={{ background: 'none', border: 'none', color: CC.primary, fontWeight: 700, cursor: 'pointer', fontSize: 13 }}>Resend OTP</button>
            )}
          </div>

          <button onClick={onBack} style={{ marginTop: 20, background: 'none', border: 'none', color: CC.muted, fontSize: 13, cursor: 'pointer' }}>← Back to Login</button>
        </div>
      </div>
    </div>
  );
}

// ─── Forgot Password Screen ──────────────────────────────────────────────────
function ForgotPasswordScreen({ onBack }) {
  const [step, setStep] = useState(1); // 1=email, 2=success
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSend = () => {
    if (!email) return;
    setLoading(true);
    setTimeout(() => { setLoading(false); setStep(2); }, 1200);
  };

  return (
    <div style={{ display: 'flex', height: '100vh', width: '100vw' }}>
      <div style={{ width: '45%', background: `linear-gradient(160deg, ${CC.navy} 0%, #0D2456 60%, #1565C0 100%)`, display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: 48 }}>
        <div style={{ textAlign: 'center', color: '#fff' }}>
          <div style={{ fontSize: 56, marginBottom: 20 }}>📧</div>
          <div style={{ fontSize: 24, fontWeight: 800, marginBottom: 10 }}>Password Reset</div>
          <div style={{ fontSize: 14, color: 'rgba(255,255,255,0.6)', maxWidth: 260, lineHeight: 1.6 }}>
            We'll send a secure reset link to your registered email address within seconds.
          </div>
        </div>
      </div>
      <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', background: CC.bg, padding: 32 }}>
        <div className="fade-in" style={{ width: '100%', maxWidth: 400 }}>
          {step === 1 ? (
            <>
              <div style={{ fontSize: 22, fontWeight: 800, color: CC.text, marginBottom: 8 }}>Reset your password</div>
              <div style={{ fontSize: 14, color: CC.muted, marginBottom: 28 }}>Enter your registered email to receive a reset link.</div>
              <AuthInput label="Email Address" type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="name@hospital.com" icon="✉️" />
              <AuthBtn onClick={handleSend} disabled={loading} style={{ marginBottom: 16 }}>
                {loading ? 'Sending...' : 'Send Reset Link'}
              </AuthBtn>
            </>
          ) : (
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: 56, marginBottom: 16 }}>✅</div>
              <div style={{ fontSize: 20, fontWeight: 800, color: CC.text, marginBottom: 8 }}>Check your inbox!</div>
              <div style={{ fontSize: 14, color: CC.muted, marginBottom: 28, lineHeight: 1.6 }}>
                A password reset link has been sent to <strong>{email}</strong>. It expires in 15 minutes.
              </div>
              <div style={{ padding: '12px 16px', background: CC.sky, borderRadius: 10, fontSize: 13, color: CC.primary, marginBottom: 24, textAlign: 'left' }}>
                <strong>Tip:</strong> Check your spam folder if you don't see the email within 2 minutes.
              </div>
            </div>
          )}
          <button onClick={onBack} style={{ background: 'none', border: 'none', color: CC.muted, fontSize: 13, cursor: 'pointer', display: 'block' }}>← Back to Login</button>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { LoginScreen, OTPScreen, ForgotPasswordScreen, CC });
