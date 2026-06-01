// CloudClinic – Main App Router
const { useState, useEffect } = React;

function App() {
  const [screen, setScreen] = useState('login');
  const [role, setRole]     = useState('doctor');
  const [page, setPage]     = useState('dashboard');
  const [pageCtx, setPageCtx] = useState(null);
  const [activeClinic, setActiveClinic] = useState(null); // for doctor multi-clinic flow

  useEffect(() => {
    const saved = localStorage.getItem('cc_session');
    if (saved) {
      const { role: r, page: p, clinic: c } = JSON.parse(saved);
      setRole(r); setPage(p || 'dashboard'); setScreen('app');
      if (c) setActiveClinic(c);
    }
  }, []);

  const saveSession = (extras = {}) => {
    const cur = JSON.parse(localStorage.getItem('cc_session') || '{}');
    localStorage.setItem('cc_session', JSON.stringify({ ...cur, role, page, clinic: activeClinic, ...extras }));
  };

  const handleLogin = (selectedRole) => { setRole(selectedRole); setScreen('otp'); };

  const handleVerify = () => {
    setScreen('app');
    if (role === 'doctor') {
      // Doctor lands on clinic picker first
      setActiveClinic(null);
      localStorage.setItem('cc_session', JSON.stringify({ role, page: 'clinic-landing', clinic: null }));
      setPage('clinic-landing');
    } else {
      setPage('dashboard');
      localStorage.setItem('cc_session', JSON.stringify({ role, page: 'dashboard' }));
    }
  };

  const handleEnterClinic = (clinic) => {
    setActiveClinic(clinic);
    setPage('dashboard');
    localStorage.setItem('cc_session', JSON.stringify({ role, page: 'dashboard', clinic }));
  };

  const handleSwitchClinic = () => {
    setActiveClinic(null);
    setPage('clinic-landing');
    localStorage.setItem('cc_session', JSON.stringify({ role, page: 'clinic-landing', clinic: null }));
  };

  const handleNav = (p, ctx) => {
    setPage(p); setPageCtx(ctx || null);
    const cur = JSON.parse(localStorage.getItem('cc_session') || '{}');
    localStorage.setItem('cc_session', JSON.stringify({ ...cur, page: p }));
  };

  const handleLogout = () => {
    localStorage.removeItem('cc_session');
    setActiveClinic(null);
    setScreen('login');
    setPage('dashboard');
  };

  if (screen === 'login')  return <LoginScreen onLogin={handleLogin} onForgot={() => setScreen('forgot')} />;
  if (screen === 'otp')    return <OTPScreen role={role} onVerify={handleVerify} onBack={() => setScreen('login')} />;
  if (screen === 'forgot') return <ForgotPasswordScreen onBack={() => setScreen('login')} />;

  // Doctor: show clinic landing if no clinic selected yet
  if (role === 'doctor' && (!activeClinic || page === 'clinic-landing')) {
    return <DoctorClinicLanding onEnter={handleEnterClinic} onLogout={handleLogout} />;
  }

  const renderPage = () => {
    switch (page) {
      case 'dashboard':
        if (role === 'doctor')       return <DoctorDashboard onNav={handleNav} clinic={activeClinic} />;
        if (role === 'receptionist') return <ReceptionDashboard onNav={handleNav} />;
        if (role === 'admin')        return <AdminDashboard onNav={handleNav} />;
        return <PatientDashboard onNav={handleNav} />;
      case 'appointments':       return <AppointmentsPage onNav={handleNav} />;
      case 'prescriptions-list': return <PrescriptionsListPage onNav={handleNav} initialFilter={pageCtx} />;
      case 'print-rx': {
        const rx = pageCtx?.inline || PRESCRIPTIONS_DB.find(p => p.id === pageCtx?.rxId);
        if (!rx) return <div style={{ padding: 40, textAlign: 'center' }}>Prescription not found.</div>;
        return <PrintPrescription rx={rx} onBack={() => handleNav('prescriptions-list')} />;
      }
      case 'patients':     return <PatientRegistration />;
      case 'opd':          return <OPDModule onNav={handleNav} role={role} />;
      case 'ipd':          return <IPDModule />;
      case 'admin-wards':  return <AdminWardsModule />;
      case 'prescription': return <PrescriptionModule ctx={pageCtx} onNav={handleNav} />;
      case 'medicine':     return <MedicineModule />;
      default:             return <DoctorDashboard onNav={handleNav} clinic={activeClinic} />;
    }
  };

  if (page === 'print-rx') {
    return <div style={{ height: '100vh', width: '100vw', overflow: 'auto', background: '#E5E7EB' }}>{renderPage()}</div>;
  }

  return (
    <div style={{ display: 'flex', height: '100vh', width: '100vw', overflow: 'hidden', background: CC.bg }}>
      <Sidebar role={role} active={page} onNav={handleNav} onLogout={handleLogout} clinic={activeClinic} onSwitchClinic={handleSwitchClinic} />
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <TopBar role={role} screen={page} clinic={activeClinic} onSwitchClinic={handleSwitchClinic} />
        <div style={{ flex: 1, overflowY: 'auto', background: CC.bg }}>
          {renderPage()}
        </div>
      </div>
    </div>
  );
}

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(<App />);
