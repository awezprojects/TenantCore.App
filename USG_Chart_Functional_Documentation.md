# USG Pregnancy Monitoring Chart — Functional Documentation

> **File:** `App_Code/QueryClass.cs` → `CalculateUSGdate(lmps, edds)`  
> **Module:** Nursing / Obstetrics  
> **Last Reviewed:** May 2026

---

## 1. Overview

The **USG (Ultrasonography) Pregnancy Monitoring Chart** is a **personalised schedule of medical appointments and tests** generated for a pregnant patient at the time her pregnancy dates are registered.

It tells the patient and the attending doctor:
- **What** medical test, scan, or vaccination is due
- **When exactly** (calendar date) it should be done
- **Why** it is being done (medical indication)

The chart covers the entire antenatal journey — from the very first viability scan at **8 weeks** through to the final growth assessment at **35 weeks**.

---

## 2. Inputs Required

Two dates must be provided to generate the chart:

| Input | Full Name | Meaning |
|-------|-----------|---------|
| **LMP** | Last Menstrual Period | The first day of the patient's last menstrual cycle. This is the universal anchor point from which all pregnancy weeks are counted. |
| **EDD** | Expected Delivery Date | The estimated due date — typically 40 weeks (280 days) from the LMP. |

> The **LMP is the single most important date** in the chart. Every scheduled appointment is calculated as a fixed number of days counted forward from this date.

---

## 3. The No-Sunday Rule

The clinic does not conduct appointments on **Sundays**.

If any calculated appointment date falls on a Sunday, the system **automatically shifts it forward by one day to Monday**. This happens silently in the background — the patient receives a Monday date without needing to make any manual adjustment.

---

## 4. How Each Appointment Date Is Calculated

Starting from the **LMP date**, the system adds a fixed number of days to arrive at each clinical milestone. The day offsets are based on standard obstetric guidelines.

| # | Pregnancy Milestone | Days Added to LMP | Resulting Week |
|---|---------------------|:-----------------:|:--------------:|
| 1 | First Viability Scan | +56 days | 8 weeks |
| 2 | Blood Test (B-HCG & PAPP-A) | +77 days | 11 weeks |
| 3 | NT Scan (Nuchal Translucency) | +87 days | 12 weeks 3 days |
| 4 | Anomaly Scan | +133 days | 19 weeks |
| 5 | Fetal Echo + GTT (1st) | +161 days | 23 weeks |
| 6 | Influenza Vaccination | +182 days | 26 weeks |
| 7 | Growth Scan (1st) | +210 days | 30 weeks |
| 8 | GTT (2nd) + Tdap Injection | +224 days | 32 weeks |
| 9 | Final Growth Scan | +245 days | 35 weeks |

> **Note:** Dates for **34 weeks** (+238 days) and **40 weeks / EDD** (+280 days) are also internally calculated but are **not printed on the patient chart** — they are reserved for internal reference or future use.

---

## 5. The Appointment Schedule — Row by Row

The chart is made up of **11 rows**, each representing one scheduled event.

---

### Row 1 — 8 Weeks · Viability Ultrasound

| Field | Detail |
|-------|--------|
| **What to do** | USG (Ultrasound Scan) |
| **Why** | Confirm Viability |

**Purpose:**  
The very first scan of the pregnancy. It confirms that the embryo is implanted correctly in the uterus, has a heartbeat, and that the pregnancy is progressing normally. It also establishes or confirms the gestational age.

---

### Row 2 — 11 Weeks · Chromosomal Screening Blood Test

| Field | Detail |
|-------|--------|
| **What to do** | Blood Test — B-HCG & PAPP-A |
| **Why** | Screening for Trisomies |

**Purpose:**  
A blood test measuring two specific hormones (Beta-HCG and Pregnancy-Associated Plasma Protein-A). Abnormal levels can indicate a higher risk of chromosomal conditions such as **Down Syndrome (Trisomy 21)**, Trisomy 18, or Trisomy 13. This is part of the first-trimester combined screening.

---

### Row 3 — 12 Weeks 3 Days · NT Scan

| Field | Detail |
|-------|--------|
| **What to do** | USG (Ultrasound Scan) |
| **Why** | NT Scan (Nuchal Translucency Scan) |

**Purpose:**  
An ultrasound that measures the fluid-filled space at the back of the baby's neck. A thicker-than-normal measurement can indicate a higher likelihood of chromosomal abnormalities. This scan is typically combined with the blood test result from Row 2 to generate a risk score.

---

### Row 4 — 19 Weeks · Anomaly Scan

| Field | Detail |
|-------|--------|
| **What to do** | USG (Ultrasound Scan) |
| **Why** | Anomaly Scan & Colour Doppler |

**Purpose:**  
The most comprehensive ultrasound of the pregnancy. The sonographer checks all major organs and body structures of the baby — brain, heart, spine, kidneys, limbs — to identify any structural abnormalities. The Colour Doppler additionally checks blood flow through the umbilical cord and placenta.

---

### Row 5 — 23 Weeks · Fetal Echo

| Field | Detail |
|-------|--------|
| **What to do** | USG (Ultrasound Scan) |
| **Why** | Fetal Echo (If Indicated) |

**Purpose:**  
A detailed ultrasound focused specifically on the **baby's heart** — its structure, chambers, valves, and blood flow. This is ordered when there is a clinical reason to suspect a congenital heart defect (e.g., a family history, an abnormal finding at the anomaly scan, or a positive chromosomal screening result).

---

### Row 6 — 23 Weeks · GTT (First)

| Field | Detail |
|-------|--------|
| **What to do** | GTT (Glucose Tolerance Test) |
| **Why** | 75 grams tolerance test for GDM |

**Purpose:**  
The patient drinks a 75-gram glucose solution and blood sugar levels are measured over time. This screens for **Gestational Diabetes Mellitus (GDM)** — a form of high blood sugar that develops during pregnancy and can cause complications for both mother and baby if unmanaged.

---

### Row 7 — 26 Weeks · Influenza Vaccination

| Field | Detail |
|-------|--------|
| **What to do** | Influenza Vaccination |
| **Why** | Protection of mother & her child up to 6 months |

**Purpose:**  
The flu vaccine is given to the mother during pregnancy. The antibodies she develops cross the placenta and also pass through breast milk, providing the **newborn with passive immunity against influenza for up to 6 months** after birth — during the period when the baby is too young to be vaccinated directly.

---

### Row 8 — 30 Weeks · Growth Scan

| Field | Detail |
|-------|--------|
| **What to do** | USG (Ultrasound Scan) |
| **Why** | Growth Scan & Colour Doppler |

**Purpose:**  
As the baby enters the third trimester, this scan checks that growth is on track — measuring the baby's head, abdomen, and femur length to estimate weight. The Colour Doppler re-assesses blood flow through the umbilical cord and placenta to ensure the baby is receiving adequate nutrition and oxygen.

---

### Row 9 — 32 Weeks · GTT (Second)

| Field | Detail |
|-------|--------|
| **What to do** | GTT (Glucose Tolerance Test) |
| **Why** | 75 grams tolerance test for GDM |

**Purpose:**  
A repeat glucose tolerance test for patients who tested negative earlier or to monitor those already diagnosed with GDM. Blood sugar management becomes especially critical in the third trimester.

---

### Row 10 — 32 Weeks · Tdap Injection

| Field | Detail |
|-------|--------|
| **What to do** | Inj Tdap (Tetanus, Diphtheria, Pertussis) |
| **Why** | Protects the newborn from pertussis, whooping cough & tetanus |

**Purpose:**  
The Tdap vaccine is given to the mother so that her body produces antibodies that are passed to the baby before birth. This protects the **newborn from whooping cough (pertussis) and tetanus** during the first weeks of life — before the baby is old enough to receive their own vaccinations.

---

### Row 11 — 35 Weeks · Final Growth Scan

| Field | Detail |
|-------|--------|
| **What to do** | USG (Ultrasound Scan) |
| **Why** | Growth Scan, Colour Doppler & Liquor Assessment |

**Purpose:**  
The last major ultrasound before delivery. In addition to checking the baby's growth and blood flow (Colour Doppler), this scan also measures the **amniotic fluid (liquor) levels**. Abnormally low fluid (oligohydramnios) or high fluid (polyhydramnios) can indicate complications that require closer monitoring or early delivery planning.

---

## 6. Chart Output Structure

The final chart delivered to the doctor and patient has **5 columns** and **11 rows**:

| Column | Description |
|--------|-------------|
| **Weeks** | The gestational week at which the appointment is due |
| **Date** | The exact calendar date (adjusted for Sundays) |
| **Day** | The day of the week (e.g., Monday, Wednesday) |
| **Things to do** | The test, scan, or procedure to be performed |
| **Indication** | The medical reason — what is being checked or protected against |

---

## 7. Complete Schedule at a Glance

| Week | Date Basis | Activity | Medical Reason |
|------|-----------|----------|----------------|
| **8 weeks** | LMP + 56 days | USG | Confirm Viability |
| **11 weeks** | LMP + 77 days | Blood Test — B-HCG & PAPP-A | Screening for Trisomies |
| **12 weeks 3 days** | LMP + 87 days | USG | NT Scan |
| **19 weeks** | LMP + 133 days | USG | Anomaly Scan & Colour Doppler |
| **23 weeks** | LMP + 161 days | USG | Fetal Echo (If Indicated) |
| **23 weeks** | LMP + 161 days | GTT | 75g glucose test for GDM |
| **26 weeks** | LMP + 182 days | Influenza Vaccination | Protect mother & baby up to 6 months |
| **30 weeks** | LMP + 210 days | USG | Growth Scan & Colour Doppler |
| **32 weeks** | LMP + 224 days | GTT | 75g glucose test for GDM (repeat) |
| **32 weeks** | LMP + 224 days | Inj Tdap | Protect newborn from whooping cough & tetanus |
| **35 weeks** | LMP + 245 days | USG | Growth Scan, Colour Doppler & Liquor Assessment |

---

## 8. Summary

> Given the date of a patient's **Last Menstrual Period (LMP)**, the system automatically calculates the exact calendar date for every important scan, blood test, and vaccination across the full pregnancy — from **8 weeks to 35 weeks** — shifting any Sunday dates to Monday, and produces a ready-to-hand-to-the-patient appointment schedule with 11 milestone events covering viability confirmation, chromosomal screening, anomaly detection, diabetes monitoring, and newborn protection vaccines.

---

*Document generated from source: `App_Code/QueryClass.cs` · Function: `CalculateUSGdate(string lmps, string edds)`*
