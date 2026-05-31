// CloudClinic – Medicine Management
const { useState } = React;

const MEDICINE_DB = [
  { id: 1,  name: 'Amlodipine',      generic: 'Amlodipine Besylate', category: 'Cardiovascular', form: 'Tablet',    strength: '5mg',    stock: 240, unit: 'strips', reorder: 50,  expiry: '2026-08', price: 4.50,   status: 'adequate' },
  { id: 2,  name: 'Atorvastatin',    generic: 'Atorvastatin Calcium', category: 'Cardiovascular', form: 'Tablet',    strength: '20mg',   stock: 310, unit: 'strips', reorder: 60,  expiry: '2026-11', price: 6.00,   status: 'adequate' },
  { id: 3,  name: 'Metoprolol',      generic: 'Metoprolol Succinate', category: 'Cardiovascular', form: 'Tablet',    strength: '50mg',   stock: 180, unit: 'strips', reorder: 50,  expiry: '2026-09', price: 5.20,   status: 'adequate' },
  { id: 4,  name: 'Aspirin',         generic: 'Acetylsalicylic Acid', category: 'Cardiovascular', form: 'Tablet',    strength: '75mg',   stock: 15,  unit: 'strips', reorder: 50,  expiry: '2027-01', price: 2.00,   status: 'low' },
  { id: 5,  name: 'Paracetamol',     generic: 'Acetaminophen',        category: 'Analgesics',      form: 'Tablet',    strength: '500mg',  stock: 500, unit: 'strips', reorder: 100, expiry: '2026-06', price: 1.50,   status: 'expiring' },
  { id: 6,  name: 'Metformin',       generic: 'Metformin HCl',        category: 'Antidiabetics',   form: 'Tablet',    strength: '500mg',  stock: 420, unit: 'strips', reorder: 80,  expiry: '2027-03', price: 3.00,   status: 'adequate' },
  { id: 7,  name: 'Omeprazole',      generic: 'Omeprazole',           category: 'GI',              form: 'Capsule',   strength: '20mg',   stock: 260, unit: 'strips', reorder: 60,  expiry: '2026-12', price: 5.50,   status: 'adequate' },
  { id: 8,  name: 'Amoxicillin',     generic: 'Amoxicillin Trihydrate',category:'Antibiotics',     form: 'Capsule',   strength: '500mg',  stock: 90,  unit: 'strips', reorder: 80,  expiry: '2026-08', price: 8.00,   status: 'adequate' },
  { id: 9,  name: 'Azithromycin',    generic: 'Azithromycin Dihydrate',category:'Antibiotics',     form: 'Tablet',    strength: '500mg',  stock: 40,  unit: 'strips', reorder: 60,  expiry: '2026-05', price: 18.00,  status: 'expiring' },
  { id: 10, name: 'Clopidogrel',     generic: 'Clopidogrel Bisulphate',category:'Cardiovascular',  form: 'Tablet',    strength: '75mg',   stock: 200, unit: 'strips', reorder: 50,  expiry: '2027-02', price: 12.00,  status: 'adequate' },
  { id: 11, name: 'Insulin Glargine',generic: 'Insulin Glargine',     category: 'Antidiabetics',   form: 'Injection', strength: '100U/mL',stock: 18,  unit: 'vials',  reorder: 20,  expiry: '2026-07', price: 450.00, status: 'low' },
  { id: 12, name: 'Pantoprazole',    generic: 'Pantoprazole Sodium',  category: 'GI',              form: 'Tablet',    strength: '40mg',   stock: 190, unit: 'strips', reorder: 50,  expiry: '2026-10', price: 7.00,   status: 'adequate' },
  { id: 13, name: 'Furosemide',      generic: 'Furosemide',           category: 'Diuretics',       form: 'Tablet',    strength: '40mg',   stock: 8,   unit: 'strips', reorder: 30,  expiry: '2026-09', price: 3.50,   status: 'low' },
  { id: 14, name: 'Losartan',        generic: 'Losartan Potassium',   category: 'Cardiovascular',  form: 'Tablet',    strength: '50mg',   stock: 150, unit: 'strips', reorder: 50,  expiry: '2026-11', price: 9.00,   status: 'adequate' },
  { id: 15, name: 'Ramipril',        generic: 'Ramipril',             category: 'Cardiovascular',  form: 'Capsule',   strength: '5mg',    stock: 110, unit: 'strips', reorder: 40,  expiry: '2027-04', price: 7.50,   status: 'adequate' },
];

const CATEGORIES = ['All', 'Cardiovascular', 'Analgesics', 'Antidiabetics', 'GI', 'Antibiotics', 'Diuretics'];

const STATUS_META_MED = {
  adequate: { label: 'In Stock',    c: CC.success, bg: '#ECFDF5' },
  low:      { label: 'Low Stock',   c: CC.error,   bg: '#FEF2F2' },
  expiring: { label: 'Expiring',    c: CC.warning, bg: '#FFFBEB' },
  out:      { label: 'Out of Stock',c: '#6B7280',  bg: '#F3F4F6' },
};

function MedicineModule() {
  const [drugs, setDrugs]         = useState(MEDICINE_DB);
  const [search, setSearch]       = useState('');
  const [category, setCategory]   = useState('All');
  const [statusFilter, setStatusF]= useState('all');
  const [showAdd, setShowAdd]     = useState(false);
  const [selected, setSelected]   = useState(null);
  const [newDrug, setNewDrug]     = useState({ name: '', generic: '', category: 'Cardiovascular', form: 'Tablet', strength: '', stock: '', reorder: '', expiry: '', price: '' });

  const filtered = drugs.filter(d => {
    const matchSearch = d.name.toLowerCase().includes(search.toLowerCase()) || d.generic.toLowerCase().includes(search.toLowerCase());
    const matchCat    = category === 'All' || d.category === category;
    const matchStatus = statusFilter === 'all' || d.status === statusFilter;
    return matchSearch && matchCat && matchStatus;
  });

  const counts = {
    total: drugs.length,
    low: drugs.filter(d => d.status === 'low').length,
    expiring: drugs.filter(d => d.status === 'expiring').length,
    adequate: drugs.filter(d => d.status === 'adequate').length,
  };

  const handleAdd = () => {
    setDrugs(ds => [...ds, { ...newDrug, id: Date.now(), stock: parseInt(newDrug.stock)||0, reorder: parseInt(newDrug.reorder)||20, price: parseFloat(newDrug.price)||0, unit: 'strips', status: parseInt(newDrug.stock) < parseInt(newDrug.reorder) ? 'low' : 'adequate' }]);
    setShowAdd(false);
    setNewDrug({ name: '', generic: '', category: 'Cardiovascular', form: 'Tablet', strength: '', stock: '', reorder: '', expiry: '', price: '' });
  };

  const setNF = (k, v) => setNewDrug(d => ({ ...d, [k]: v }));

  return (
    <div className="fade-in" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 20 }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ fontSize: 18, fontWeight: 800 }}>Medicine Management</div>
          <div style={{ fontSize: 13, color: CC.muted }}>Drug formulary & inventory</div>
        </div>
        <Btn onClick={() => setShowAdd(true)}>+ Add New Drug</Btn>
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 14 }}>
        <StatCard label="Total Drugs"  value={counts.total}    icon="💊" color="#EFF6FF" />
        <StatCard label="Low Stock"    value={counts.low}      icon="⚠️" color="#FEF2F2" />
        <StatCard label="Expiring Soon" value={counts.expiring} icon="⏰" color="#FFFBEB" />
        <StatCard label="In Stock"     value={counts.adequate} icon="✅" color="#ECFDF5" />
      </div>

      {/* Alerts */}
      {(counts.low > 0 || counts.expiring > 0) && (
        <div style={{ display: 'flex', gap: 10 }}>
          {drugs.filter(d => d.status === 'low').map(d => (
            <div key={d.id} style={{ padding: '10px 14px', background: '#FEF2F2', borderRadius: 10, border: `1px solid #FECACA`, fontSize: 13, display: 'flex', alignItems: 'center', gap: 8 }}>
              <span>⚠️</span><strong>{d.name}</strong><span style={{ color: CC.muted }}>— only {d.stock} {d.unit} left</span>
            </div>
          ))}
          {drugs.filter(d => d.status === 'expiring').map(d => (
            <div key={d.id} style={{ padding: '10px 14px', background: '#FFFBEB', borderRadius: 10, border: `1px solid #FDE68A`, fontSize: 13, display: 'flex', alignItems: 'center', gap: 8 }}>
              <span>⏰</span><strong>{d.name}</strong><span style={{ color: CC.muted }}>— expires {d.expiry}</span>
            </div>
          ))}
        </div>
      )}

      {/* Filters */}
      <Card style={{ padding: 16 }}>
        <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center' }}>
          <div style={{ position: 'relative', flex: 1, minWidth: 200 }}>
            <span style={{ position: 'absolute', left: 11, top: '50%', transform: 'translateY(-50%)', color: CC.muted }}>🔍</span>
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search drug or generic name…" style={{ width: '100%', padding: '8px 12px 8px 34px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, outline: 'none' }} />
          </div>
          <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
            {CATEGORIES.map(c => (
              <button key={c} onClick={() => setCategory(c)} style={{ padding: '7px 12px', borderRadius: 8, border: `1.5px solid ${category === c ? CC.primary : CC.border}`, background: category === c ? CC.sky : '#fff', color: category === c ? CC.primary : CC.muted, fontWeight: category === c ? 700 : 500, fontSize: 12, cursor: 'pointer' }}>{c}</button>
            ))}
          </div>
          <select value={statusFilter} onChange={e => setStatusF(e.target.value)} style={{ padding: '8px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, outline: 'none', cursor: 'pointer' }}>
            <option value="all">All Status</option>
            <option value="adequate">In Stock</option>
            <option value="low">Low Stock</option>
            <option value="expiring">Expiring</option>
          </select>
        </div>
      </Card>

      {/* Table */}
      <Card>
        <Table
          cols={['Drug Name', 'Generic Name', 'Category', 'Form / Strength', 'Stock', 'Reorder Lvl', 'Expiry', 'Price / Unit', 'Status', 'Actions']}
          rows={filtered.map(d => {
            const sm = STATUS_META_MED[d.status];
            const stockPct = Math.min(100, (d.stock / (d.reorder * 4)) * 100);
            return { cells: [
              <div>
                <div style={{ fontWeight: 700, fontSize: 13 }}>{d.name}</div>
              </div>,
              <span style={{ fontSize: 12, color: CC.muted }}>{d.generic}</span>,
              <Badge color={CC.primary} bg={CC.sky}>{d.category}</Badge>,
              <span style={{ fontSize: 13 }}>{d.form} · <strong>{d.strength}</strong></span>,
              <div style={{ minWidth: 90 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, marginBottom: 3 }}>
                  <span style={{ fontWeight: 700 }}>{d.stock}</span>
                  <span style={{ color: CC.muted }}>{d.unit}</span>
                </div>
                <div style={{ height: 4, background: '#F1F5F9', borderRadius: 4 }}>
                  <div style={{ height: '100%', width: `${stockPct}%`, background: d.status === 'low' ? CC.error : d.status === 'expiring' ? CC.warning : CC.success, borderRadius: 4 }} />
                </div>
              </div>,
              <span style={{ fontWeight: 600, color: d.stock <= d.reorder ? CC.error : CC.muted }}>{d.reorder}</span>,
              <span style={{ fontSize: 12, color: d.status === 'expiring' ? CC.warning : CC.text, fontWeight: d.status === 'expiring' ? 700 : 400 }}>{d.expiry}</span>,
              <span style={{ fontWeight: 700, color: CC.success }}>₹{d.price.toFixed(2)}</span>,
              <Badge color={sm.c} bg={sm.bg}>{sm.label}</Badge>,
              <div style={{ display: 'flex', gap: 6 }}>
                <Btn size="sm" variant="light" onClick={() => setSelected(d)}>View</Btn>
              </div>,
            ]};
          })}
        />
      </Card>

      {/* Drug Detail Modal */}
      <Modal open={!!selected} onClose={() => setSelected(null)} title="Drug Details" width={480}>
        {selected && (
          <div>
            <div style={{ display: 'flex', gap: 16, alignItems: 'center', marginBottom: 20 }}>
              <div style={{ width: 56, height: 56, borderRadius: 14, background: CC.sky, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 28 }}>💊</div>
              <div>
                <div style={{ fontSize: 18, fontWeight: 800 }}>{selected.name}</div>
                <div style={{ fontSize: 13, color: CC.muted }}>{selected.generic}</div>
                <Badge color={STATUS_META_MED[selected.status].c} bg={STATUS_META_MED[selected.status].bg}>{STATUS_META_MED[selected.status].label}</Badge>
              </div>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, marginBottom: 16 }}>
              {[
                ['Category', selected.category],
                ['Form', selected.form],
                ['Strength', selected.strength],
                ['Current Stock', `${selected.stock} ${selected.unit}`],
                ['Reorder Level', `${selected.reorder} ${selected.unit}`],
                ['Expiry Date', selected.expiry],
                ['Price per Unit', `₹${selected.price.toFixed(2)}`],
                ['Status', STATUS_META_MED[selected.status].label],
              ].map(([k, v]) => (
                <div key={k} style={{ padding: '12px 14px', background: '#F8FAFC', borderRadius: 10 }}>
                  <div style={{ fontSize: 11, color: CC.muted, marginBottom: 2 }}>{k}</div>
                  <div style={{ fontSize: 13, fontWeight: 700 }}>{v}</div>
                </div>
              ))}
            </div>
            {/* Stock update */}
            <div style={{ padding: '14px 16px', background: CC.sky, borderRadius: 10, marginBottom: 16 }}>
              <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 10 }}>Update Stock</div>
              <div style={{ display: 'flex', gap: 10 }}>
                <input type="number" placeholder="Add qty" style={{ flex: 1, padding: '8px 12px', border: `1.5px solid ${CC.border}`, borderRadius: 8, fontSize: 13, outline: 'none' }} id={`stock-input-${selected.id}`} />
                <Btn onClick={() => {
                  const inp = document.getElementById(`stock-input-${selected.id}`);
                  const qty = parseInt(inp.value) || 0;
                  setDrugs(ds => ds.map(d => d.id === selected.id ? { ...d, stock: d.stock + qty, status: (d.stock + qty) < d.reorder ? 'low' : 'adequate' } : d));
                  setSelected(s => ({ ...s, stock: s.stock + qty }));
                  inp.value = '';
                }}>Add Stock</Btn>
              </div>
            </div>
            <Btn variant="ghost" onClick={() => setSelected(null)} style={{ width: '100%' }}>Close</Btn>
          </div>
        )}
      </Modal>

      {/* Add Drug Modal */}
      <Modal open={showAdd} onClose={() => setShowAdd(false)} title="Add New Drug to Formulary" width={520}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
          <div style={{ gridColumn: '1/-1' }}>
            <FormField label="Brand / Drug Name" required>
              <Input value={newDrug.name} onChange={e => setNF('name', e.target.value)} placeholder="e.g. Metformin" />
            </FormField>
          </div>
          <div style={{ gridColumn: '1/-1' }}>
            <FormField label="Generic Name">
              <Input value={newDrug.generic} onChange={e => setNF('generic', e.target.value)} placeholder="e.g. Metformin Hydrochloride" />
            </FormField>
          </div>
          <FormField label="Category" required>
            <Select value={newDrug.category} onChange={e => setNF('category', e.target.value)} options={CATEGORIES.filter(c => c !== 'All')} />
          </FormField>
          <FormField label="Form" required>
            <Select value={newDrug.form} onChange={e => setNF('form', e.target.value)} options={['Tablet', 'Capsule', 'Syrup', 'Injection', 'Ointment', 'Drops', 'Inhaler']} />
          </FormField>
          <FormField label="Strength" required>
            <Input value={newDrug.strength} onChange={e => setNF('strength', e.target.value)} placeholder="e.g. 500mg" />
          </FormField>
          <FormField label="Current Stock" required>
            <Input type="number" value={newDrug.stock} onChange={e => setNF('stock', e.target.value)} placeholder="Number of units/strips" />
          </FormField>
          <FormField label="Reorder Level">
            <Input type="number" value={newDrug.reorder} onChange={e => setNF('reorder', e.target.value)} placeholder="Min stock before alert" />
          </FormField>
          <FormField label="Expiry (YYYY-MM)">
            <Input value={newDrug.expiry} onChange={e => setNF('expiry', e.target.value)} placeholder="2027-06" />
          </FormField>
          <FormField label="Price per Unit (₹)">
            <Input type="number" value={newDrug.price} onChange={e => setNF('price', e.target.value)} placeholder="0.00" />
          </FormField>
        </div>
        <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end', marginTop: 16 }}>
          <Btn variant="ghost" onClick={() => setShowAdd(false)}>Cancel</Btn>
          <Btn onClick={handleAdd}>Add to Formulary</Btn>
        </div>
      </Modal>
    </div>
  );
}

Object.assign(window, { MedicineModule });
