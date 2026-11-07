// ============================================================
// NWSDB Portal - Frontend logic
// Talks to NWSDB.API over plain HTTP fetch() calls.
// Two portals: Customer and NWSDB Staff. (The Third Party Partner is NOT part of this
// client any more - partners run their own separate app, see QuickPay.PartnerApp.)
// All three now authenticate first (JWT) before using their portal,
// matching the use-case diagram's Authenticate User / Authenticate Staff /
// Authenticate API Client use cases.
// ============================================================

const API_BASE_URL = "http://localhost:5094/api";

let currentCustomerId = null;
let currentBill = null; // Customer portal: the bill currently selected on the Bill Payment page
let outstandingBills = []; // Customer portal: Unpaid + Overdue bills, oldest first (My Bills page)
let selectedPayBillId = null; // Bill picked via a "Pay Bill" button / the Bill Payment dropdown


// ---------- Auth state (kept in memory only, cleared on Switch Role) ----------
let authToken = null;
let authRole = null; // "Customer" | "Staff"
let authSubjectId = null;
let authDisplayName = null;

// ---------- Generic API helper ----------

async function apiRequest(path, options = {}) {
  const headers = { "Content-Type": "application/json", ...(options.headers || {}) };
  if (authToken) {
    headers["Authorization"] = "Bearer " + authToken;
  }

  const response = await fetch(API_BASE_URL + path, {
    ...options,
    headers,
  });

  let data = null;
  try {
    data = await response.json();
  } catch {
    data = null;
  }

  if (!response.ok) {
    const errorMessage =
      (data && (data.message || data.title)) ||
      (typeof data === "string" ? data : null) ||
      `Request failed (${response.status})`;
    throw new Error(errorMessage);
  }

  return data;
}

// ---------- Generic message banner (works for any screen) ----------

function showMessage(boxId, text, type) {
  const box = document.getElementById(boxId);
  box.textContent = text;
  box.className = "message " + type; // "success" or "error"
  box.classList.remove("hidden");

  window.scrollTo({ top: 0, behavior: "smooth" });

  setTimeout(() => {
    box.classList.add("hidden");
  }, 5000);
}

// ============================================================
// ROLE SELECTION + AUTH GATES
// ============================================================

const roleSelect = document.getElementById("roleSelect");
const logoutBtn = document.getElementById("logoutBtn");
const roleLabel = document.getElementById("roleLabel");
const publicNav = document.getElementById("publicNav");
const portals = document.querySelectorAll(".portal");
const authScreens = [
  document.getElementById("customerAuth"),
  document.getElementById("staffAuth"),
];
const publicPages = document.querySelectorAll(".public-page");

const roleNames = {
  customer: "Customer Portal",
  staff: "NWSDB Staff Portal",
};

const roleToPortalId = {
  customer: "customerPortal",
  staff: "staffPortal",
};

const roleToAuthId = {
  customer: "customerAuth",
  staff: "staffAuth",
};

function hideAll() {
  roleSelect.classList.add("hidden");
  authScreens.forEach((a) => a.classList.add("hidden"));
  portals.forEach((p) => p.classList.add("hidden"));
  publicPages.forEach((p) => p.classList.add("hidden"));
}

// Public nav: Home / About Us / Contact Us / Login -> just show that static page.
document.querySelectorAll(".nav-link[data-nav], .hero-actions [data-nav]").forEach((btn) => {
  btn.addEventListener("click", () => {
    hideAll();
    document.getElementById(btn.dataset.nav).classList.remove("hidden");
  });
});

// Register (nav bar or Home page button) -> straight to the Customer auth screen,
// with the Register form open (since only Customers self-register).
function setCustomerAuthMode(mode) {
  const loginForm = document.getElementById("customerLoginForm");
  const registerForm = document.getElementById("customerRegisterForm");
  const title = document.getElementById("customerAuthTitle");
  const hint = document.getElementById("customerAuthHint");
  const toggleToRegister = document.getElementById("toggleToRegister");
  const toggleToLogin = document.getElementById("toggleToLogin");

  clearRegisterErrors();

  if (mode === "register") {
    loginForm.classList.add("hidden");
    registerForm.classList.remove("hidden");
    toggleToRegister.classList.add("hidden");
    toggleToLogin.classList.remove("hidden");
    title.textContent = "Register Account";
    hint.textContent = "Create your account to get started.";
  } else {
    registerForm.classList.add("hidden");
    loginForm.classList.remove("hidden");
    toggleToLogin.classList.add("hidden");
    toggleToRegister.classList.remove("hidden");
    title.textContent = "Customer Login";
    hint.textContent = "Log in with your email and password.";
  }
}

function goToCustomerRegister() {
  hideAll();
  document.getElementById("customerAuth").classList.remove("hidden");
  setCustomerAuthMode("register");
}
document.getElementById("registerNavBtn").addEventListener("click", goToCustomerRegister);
document.getElementById("homeRegisterBtn").addEventListener("click", goToCustomerRegister);

// Home page "Login to Customer Portal" buttons (hero + Customer Portal section) ->
// straight to the Customer auth screen with the Login form open, same screen the
// header's Login nav item / role-select Customer card lead to.
function goToCustomerLogin() {
  hideAll();
  document.getElementById("customerAuth").classList.remove("hidden");
  setCustomerAuthMode("login");
}
document.querySelectorAll(".js-customer-login-btn").forEach((btn) => {
  btn.addEventListener("click", goToCustomerLogin);
});

// Role card -> show that role's auth screen first (not the portal directly).
document.querySelectorAll(".role-card").forEach((card) => {
  card.addEventListener("click", () => {
    const role = card.dataset.role;
    hideAll();
    if (role === "customer") setCustomerAuthMode("login");
    document.getElementById(roleToAuthId[role]).classList.remove("hidden");
  });
});

// Back button on the Staff auth screen -> role select. (The Customer Login/Register
// screen has no Back button by design; use the header nav or logout instead.)

function backToRoleSelect() {
  hideAll();
  roleSelect.classList.remove("hidden");
}

// Logout: clears the JWT and any loaded data, back to Home.
logoutBtn.addEventListener("click", () => {
  authToken = null;
  authRole = null;
  authSubjectId = null;
  authDisplayName = null;
  currentCustomerId = null;
  currentBill = null;
  outstandingBills = [];
  selectedPayBillId = null;
  resetPaymentPage();
  document.getElementById("supportForm").reset();
  document.getElementById("supportMessage").classList.add("hidden");

  hideAll();
  document.getElementById("home").classList.remove("hidden");
  logoutBtn.classList.add("hidden");
  publicNav.classList.remove("hidden");
  roleLabel.textContent = "National Water Supply & Drainage Board";
});

// Called after a successful login/register/authenticate for any role.
function enterPortal(role) {
  hideAll();
  const portalId = roleToPortalId[role];
  document.getElementById(portalId).classList.remove("hidden");

  logoutBtn.classList.remove("hidden");
  publicNav.classList.add("hidden");
  roleLabel.textContent = `${roleNames[role]} — ${authDisplayName}`;
}

// ---------- Tab navigation (scoped per portal) ----------

portals.forEach((portal) => {
  const buttons = portal.querySelectorAll(".tab-btn");
  const panels = portal.querySelectorAll(".tab-panel");

  buttons.forEach((btn) => {
    btn.addEventListener("click", () => {
      buttons.forEach((b) => b.classList.remove("active"));
      panels.forEach((p) => p.classList.remove("active"));

      btn.classList.add("active");
      document.getElementById(btn.dataset.tab).classList.add("active");

      // Customer portal: re-read the bill / payment data from the API whenever one of
      // these tabs is (re)visited, so it always reflects the backend (e.g. after a payment).
      if (portal.id === "customerPortal") onCustomerTabOpened(btn.dataset.tab);
    });
  });
});

// Dashboard shortcut buttons (Quick Actions, per-card "View ..." links) and any
// dynamically-inserted button (e.g. the Current Bill card's "Pay Bill") reuse the
// real tab buttons' click handling above via delegation, instead of duplicating it.
document.addEventListener("click", (e) => {
  const trigger = e.target.closest("[data-goto-tab]");
  if (!trigger) return;
  const tabBtn = document.querySelector(
    `#customerPortal .tab-btn[data-tab="${trigger.dataset.gotoTab}"]`
  );
  if (tabBtn) tabBtn.click();
});

// ============================================================
// CUSTOMER LOGIN / REGISTER  (Authenticate User)
// ============================================================

document.getElementById("showRegisterBtn").addEventListener("click", () => {
  setCustomerAuthMode("register");
});

document.getElementById("showLoginBtn").addEventListener("click", () => {
  setCustomerAuthMode("login");
});

document.getElementById("customerLoginForm").addEventListener("submit", async (e) => {
  e.preventDefault();
  const email = document.getElementById("loginCustomerEmail").value.trim();
  const password = document.getElementById("loginCustomerPassword").value;

  try {
    const auth = await apiRequest("/auth/customer/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    });
    applyAuth(auth);
    await afterCustomerAuth();
  } catch (err) {
    showMessage("customerAuthMessage", err.message, "error");
  }
});

function togglePasswordVisibility(inputId, toggleBtnId) {
  const input = document.getElementById(inputId);
  const btn = document.getElementById(toggleBtnId);
  btn.addEventListener("click", () => {
    const isHidden = input.type === "password";
    input.type = isHidden ? "text" : "password";
    btn.setAttribute("aria-label", isHidden ? "Hide password" : "Show password");
  });
}
togglePasswordVisibility("loginCustomerPassword", "loginCustomerPasswordToggle");
togglePasswordVisibility("regCustomerPassword", "regCustomerPasswordToggle");
togglePasswordVisibility("regCustomerConfirmPassword", "regCustomerConfirmPasswordToggle");
togglePasswordVisibility("loginStaffPassword", "loginStaffPasswordToggle");

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

// Register form: each field shows its own error message under it.
const REGISTER_FIELDS = {
  name: ["regCustomerName", "regNameError"],
  address: ["regCustomerAddress", "regAddressError"],
  phone: ["regCustomerPhone", "regPhoneError"],
  email: ["regCustomerEmail", "regEmailError"],
  password: ["regCustomerPassword", "regPasswordError"],
  confirmPassword: ["regCustomerConfirmPassword", "regConfirmPasswordError"],
  terms: ["regCustomerTerms", "regTermsError"],
};

// Sri Lankan style numbers: 0 + 9 digits (e.g. 0771234567) or +94 + 9 digits.
const PHONE_REGEX = /^(?:0|\+94)\d{9}$/;

function setRegisterFieldError(field, message) {
  const [inputId, errorId] = REGISTER_FIELDS[field];
  const input = document.getElementById(inputId);
  document.getElementById(errorId).textContent = message || "";
  input.classList.toggle("input-invalid", Boolean(message));
  input.setAttribute("aria-invalid", message ? "true" : "false");
}

function clearRegisterErrors() {
  Object.keys(REGISTER_FIELDS).forEach((field) => setRegisterFieldError(field, ""));
}

// An error disappears as soon as the customer edits that field.
Object.entries(REGISTER_FIELDS).forEach(([field, [inputId]]) => {
  const input = document.getElementById(inputId);
  input.addEventListener(field === "terms" ? "change" : "input", () => {
    setRegisterFieldError(field, "");
    if (field === "password" || field === "confirmPassword") setRegisterFieldError("confirmPassword", "");
  });
});

// Returns { field: message } for every invalid field (empty object = form is valid).
function validateRegisterForm(values) {
  const errors = {};

  if (!values.name) errors.name = "Name is required.";
  if (!values.address) errors.address = "Address is required.";

  if (!values.phone) {
    errors.phone = "Phone number is required.";
  } else if (!PHONE_REGEX.test(values.phone.replace(/[\s-]/g, ""))) {
    errors.phone = "Please enter a valid phone number (e.g. 0771234567).";
  }

  if (!values.email) {
    errors.email = "Email address is required.";
  } else if (!EMAIL_REGEX.test(values.email)) {
    errors.email = "Please enter a valid email address.";
  }

  if (!values.password) {
    errors.password = "Password is required.";
  } else if (values.password.length < 4) {
    errors.password = "Password must be at least 4 characters long.";
  }

  if (!values.confirmPassword) {
    errors.confirmPassword = "Please confirm your password.";
  } else if (values.password !== values.confirmPassword) {
    errors.confirmPassword = "Passwords do not match.";
  }

  if (!values.terms) errors.terms = "You must agree to the Terms and Conditions to register.";

  return errors;
}

document.getElementById("customerRegisterForm").addEventListener("submit", async (e) => {
  e.preventDefault();
  const payload = {
    name: document.getElementById("regCustomerName").value.trim(),
    address: document.getElementById("regCustomerAddress").value.trim(),
    phone: document.getElementById("regCustomerPhone").value.replace(/[\s-]/g, ""),
    email: document.getElementById("regCustomerEmail").value.trim(),
    password: document.getElementById("regCustomerPassword").value,
  };

  const errors = validateRegisterForm({
    ...payload,
    confirmPassword: document.getElementById("regCustomerConfirmPassword").value,
    terms: document.getElementById("regCustomerTerms").checked,
  });

  clearRegisterErrors();
  const invalidFields = Object.keys(REGISTER_FIELDS).filter((field) => errors[field]);
  if (invalidFields.length) {
    invalidFields.forEach((field) => setRegisterFieldError(field, errors[field]));
    document.getElementById(REGISTER_FIELDS[invalidFields[0]][0]).focus();
    return;
  }

  try {
    const auth = await apiRequest("/auth/customer/register", {
      method: "POST",
      body: JSON.stringify(payload),
    });
    // Account created — send them to the login form to sign in, rather than
    // logging them in automatically. Login now uses email, so pre-fill that
    // field with the email they just registered with.
    document.getElementById("customerRegisterForm").reset();
    setCustomerAuthMode("login");
    document.getElementById("loginCustomerEmail").value = payload.email;
    showMessage(
      "customerAuthMessage",
      `Account created. Your Customer ID is ${auth.subjectId} — please note it down. Please log in.`,
      "success"
    );
  } catch (err) {
    // Duplicate-email registration error: show it right under the Email field
    // (same styling as the other field-level validation errors above), not just
    // the generic banner, so it's obvious which field caused the problem.
    if (/account with this email already exists/i.test(err.message)) {
      setRegisterFieldError("email", err.message);
      document.getElementById("regCustomerEmail").focus();
    } else {
      showMessage("customerAuthMessage", err.message, "error");
    }
  }
});

function applyAuth(auth) {
  authToken = auth.token;
  authRole = auth.role;
  authSubjectId = auth.subjectId;
  authDisplayName = auth.displayName;
}

async function afterCustomerAuth() {
  enterPortal("customer");
  // Customers only ever view their own account, so load it straight from the JWT.
  await loadCustomer();
}

// ============================================================
// STAFF LOGIN (Authenticate Staff)
// ============================================================

document.getElementById("staffLoginForm").addEventListener("submit", async (e) => {
  e.preventDefault();
  const email = document.getElementById("loginStaffEmail").value.trim();
  const password = document.getElementById("loginStaffPassword").value;

  try {
    const auth = await apiRequest("/auth/staff/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    });
    applyAuth(auth);
    enterPortal("staff");
    showMessage("staffMessageBox", `Login successful. Welcome, ${auth.displayName}!`, "success");
  } catch (err) {
    showMessage("staffAuthMessage", err.message, "error");
  }
});

// ============================================================
// CUSTOMER PORTAL
// ============================================================

let currentCustomerData = null; // last-loaded profile, used to repopulate the edit form / Cancel

async function loadCustomer() {
  const id = authSubjectId;

  if (!id) {
    showMessage("messageBox", "You are not logged in.", "error");
    return;
  }

  try {
    const customer = await apiRequest(`/customer/${id}`);
    currentCustomerId = customer.customerId;
    currentCustomerData = customer;

    renderCustomerDetails(customer);
    document.getElementById("dashCustomerName").textContent = customer.name;
    document.getElementById("dashCustomerId").textContent = `Customer ID: ${customer.customerId}`;

    await Promise.all([loadCurrentBill(id), loadWaterUsage(id), loadPaymentHistory(id), loadSupportRequests(id)]);

    showMessage("messageBox", `Customer ${customer.customerId} loaded successfully.`, "success");
  } catch (err) {
    currentCustomerId = null;
    currentCustomerData = null;
    showMessage("messageBox", err.message, "error");
  }
}

// Renders the Profile Details card in either read-only "view" mode (default) or
// "edit" mode. Customer ID and Email stay plain read-only text in BOTH modes -
// only Name, Address and Phone ever become input fields.
function renderCustomerDetails(customer, editMode = false) {
  const card = document.getElementById("customerDetailCard");

  if (!editMode) {
    card.innerHTML = `
      <p><span class="label">Customer ID:</span> ${customer.customerId}</p>
      <p><span class="label">Name:</span> ${customer.name}</p>
      <p><span class="label">Address:</span> ${customer.address}</p>
      <p><span class="label">Phone:</span> ${customer.phone}</p>
      <p><span class="label">Email Address:</span> ${customer.email}</p>
      <div class="profile-actions">
        <button type="button" class="btn-primary" id="editProfileBtn">Edit Profile</button>
      </div>
    `;
    document.getElementById("editProfileBtn").addEventListener("click", () => {
      document.getElementById("profileMessage").classList.add("hidden");
      renderCustomerDetails(currentCustomerData, true);
    });
    return;
  }

  card.innerHTML = `
    <p><span class="label">Customer ID:</span> ${customer.customerId}</p>
    <form id="profileEditForm" class="stacked-form" novalidate>
      <label for="profileName">Name</label>
      <input type="text" id="profileName" value="${escapeAttr(customer.name)}" />
      <div class="field-error" id="profileNameError"></div>

      <label for="profileAddress">Address</label>
      <input type="text" id="profileAddress" value="${escapeAttr(customer.address)}" />
      <div class="field-error" id="profileAddressError"></div>

      <label for="profilePhone">Phone</label>
      <input type="text" id="profilePhone" value="${escapeAttr(customer.phone)}" />
      <div class="field-error" id="profilePhoneError"></div>

      <label for="profileEmail">Email Address</label>
      <input type="email" id="profileEmail" value="${escapeAttr(customer.email)}" readonly disabled />
      <span class="section-hint">Email is your login ID and cannot be changed here.</span>

      <div class="profile-actions">
        <button type="submit" class="btn-primary" id="saveProfileBtn">Save Changes</button>
        <button type="button" class="btn-outline" id="cancelProfileBtn">Cancel</button>
      </div>
    </form>
  `;

  const PROFILE_FIELDS = {
    name: ["profileName", "profileNameError"],
    address: ["profileAddress", "profileAddressError"],
    phone: ["profilePhone", "profilePhoneError"],
  };

  const setProfileFieldError = (field, message) => {
    const [inputId, errorId] = PROFILE_FIELDS[field];
    const input = document.getElementById(inputId);
    document.getElementById(errorId).textContent = message || "";
    input.classList.toggle("input-invalid", Boolean(message));
    input.setAttribute("aria-invalid", message ? "true" : "false");
  };

  Object.entries(PROFILE_FIELDS).forEach(([field, [inputId]]) => {
    document.getElementById(inputId).addEventListener("input", () => setProfileFieldError(field, ""));
  });

  document.getElementById("cancelProfileBtn").addEventListener("click", () => {
    renderCustomerDetails(currentCustomerData, false);
  });

  document.getElementById("profileEditForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const values = {
      name: document.getElementById("profileName").value.trim(),
      address: document.getElementById("profileAddress").value.trim(),
      phone: document.getElementById("profilePhone").value.trim(),
    };

    const errors = {};
    if (!values.name) errors.name = "Name is required.";
    if (!values.address) errors.address = "Address is required.";
    if (!values.phone) {
      errors.phone = "Phone number is required.";
    } else if (!PHONE_REGEX.test(values.phone.replace(/[\s-]/g, ""))) {
      errors.phone = "Please enter a valid phone number (e.g. 0771234567).";
    }

    const invalidFields = Object.keys(PROFILE_FIELDS).filter((field) => errors[field]);
    if (invalidFields.length) {
      invalidFields.forEach((field) => setProfileFieldError(field, errors[field]));
      document.getElementById(PROFILE_FIELDS[invalidFields[0]][0]).focus();
      return;
    }

    const saveBtn = document.getElementById("saveProfileBtn");
    saveBtn.disabled = true;
    saveBtn.textContent = "Saving...";

    try {
      // Customer ID is never sent - the backend takes it from the authenticated
      // token (see PUT /api/customer/me), so this can never modify another customer.
      const updated = await apiRequest("/customer/me", {
        method: "PUT",
        // Spaces/hyphens are allowed while typing ("077 123 4567") but saved as digits only.
        body: JSON.stringify({ ...values, phone: values.phone.replace(/[\s-]/g, "") }),
      });

      currentCustomerId = updated.customerId;
      currentCustomerData = updated;
      document.getElementById("dashCustomerName").textContent = updated.name;

      renderCustomerDetails(updated, false);
      showMessage("profileMessage", "Profile updated successfully.", "success");
    } catch (err) {
      saveBtn.disabled = false;
      saveBtn.textContent = "Save Changes";
      showMessage("profileMessage", err.message || "Could not update profile. Please try again.", "error");
    }
  });
}

// Minimal HTML-attribute escaping so values with quotes/angle brackets can't break
// the input markup above (customer.name etc. are rendered inside value="...").
function escapeAttr(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/"/g, "&quot;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

// Formats a "YYYY-MM" billing period as "Month YYYY" (e.g. "2026-08" -> "August 2026").
// Used only for the Current Bill page — other pages keep their existing raw display.
function formatBillingPeriod(period) {
  const [year, month] = (period || "").split("-").map(Number);
  if (!year || !month) return period;
  return new Date(year, month - 1, 1).toLocaleDateString("en-US", { month: "long", year: "numeric" });
}

// Formats a currency amount with thousands separators (e.g. 1250.5 -> "Rs. 1,250.50").
// Used by the Current Bill and Payment History pages.
function formatCurrencyRs(amount) {
  return `Rs. ${amount.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

// Unpaid + Overdue bills, oldest first. Same order the API uses for the "current" bill
// (due date, then Bill ID), so the oldest bill here is the one the API will accept first.
function sortOutstanding(bills) {
  return bills
    .filter((b) => !b.isPaid)
    .sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate) || a.billId - b.billId);
}

function isOldestOutstanding(bill) {
  return outstandingBills.length > 0 && outstandingBills[0].billId === bill.billId;
}

// Latest bill = the one for the most recent billing period (ties broken by issued date).
// Used only for the Dashboard's Current Bill card, which shows the latest month's bill
// regardless of its paid/unpaid status — unlike My Bills / Bill Payment below, which
// always work oldest-unpaid-first.
function getLatestBill(bills) {
  if (!bills.length) return null;
  return [...bills].sort(
    (a, b) =>
      b.billingPeriod.localeCompare(a.billingPeriod) || new Date(b.issuedDate) - new Date(a.issuedDate)
  )[0];
}

// Loads every outstanding bill and fills the My Bills page, the Dashboard's bill card
// (shows the latest month's bill) and the Bill Payment page (shows the selected bill,
// default oldest unpaid).
async function loadCurrentBill(customerId) {
  const billCard = document.getElementById("billDetailCard");
  const dashBillAmount = document.getElementById("dashBillAmount");
  const dashBillStatus = document.getElementById("dashBillStatus");
  const dashBillDueDate = document.getElementById("dashBillDueDate");
  const dashViewBillBtn = document.getElementById("dashViewBillBtn");

  let bills = [];
  try {
    bills = await apiRequest(`/bill/customer/${customerId}`);
  } catch {
    // No bills for this customer — handled below as "no bills yet".
  }
  outstandingBills = sortOutstanding(bills);

  // Dashboard's Current Bill card: always the latest month's bill, whatever its status.
  const latestBill = getLatestBill(bills);
  if (latestBill) {
    const dashBadgeClass = latestBill.status.toLowerCase();
    dashBillAmount.textContent = `Rs. ${latestBill.amount.toFixed(2)}`;
    dashBillStatus.innerHTML = `<span class="badge ${dashBadgeClass}">${latestBill.status}</span>`;
    dashBillDueDate.textContent = `Due Date: ${formatReadingDate(latestBill.dueDate)}`;
    dashViewBillBtn.classList.remove("hidden");
  } else {
    dashBillAmount.textContent = "No bills yet";
    dashBillStatus.innerHTML = "";
    dashBillDueDate.textContent = "";
    dashViewBillBtn.classList.add("hidden");
  }

  if (!outstandingBills.length) {
    currentBill = null;
    billCard.innerHTML = `<div class="detail-card"><p class="section-hint">No outstanding bill for this customer. All bills are paid.</p></div>`;
    await renderPaymentUnavailable(customerId);
    return;
  }

  const oldest = outstandingBills[0];
  currentBill = outstandingBills.find((b) => b.billId === selectedPayBillId) || oldest;

  // Water usage for each bill's billing period, if any.
  let usageList = [];
  try {
    usageList = await apiRequest(`/waterusage/customer/${customerId}`);
  } catch {
    // No usage records for this customer — every bill shows the placeholder text.
  }

  const total = outstandingBills.reduce((sum, b) => sum + b.amount, 0);
  const count = outstandingBills.length;

  const summary = `
    <div class="detail-card bills-summary">
      <p class="section-hint">Total Outstanding</p>
      <p class="summary-total">${formatCurrencyRs(total)}</p>
      <p class="section-hint">${count} unpaid bill${count === 1 ? "" : "s"} — bills must be paid oldest first.</p>
    </div>`;

  const items = outstandingBills
    .map((bill, index) => {
      const match = usageList.find((u) => u.billingPeriod === bill.billingPeriod);
      const usageText = match ? `${match.unitsConsumed} m³` : "Not available";
      const badgeClass = bill.status.toLowerCase();

      const action =
        index === 0
          ? `<button type="button" class="btn-primary" data-pay-bill="${bill.billId}">Pay Bill</button>`
          : `<button type="button" class="btn-primary" disabled>Pay Bill</button>
             <p class="section-hint bill-wait-note">Pay Bill ${oldest.billId} (${formatBillingPeriod(oldest.billingPeriod)}) first.</p>`;

      return `
        <div class="detail-card bill-item">
          <p><span class="label">Bill ID:</span> ${bill.billId}</p>
          <p><span class="label">Billing Period:</span> ${formatBillingPeriod(bill.billingPeriod)}</p>
          <p><span class="label">Water Usage:</span> ${usageText}</p>
          <p><span class="label">Amount:</span> ${formatCurrencyRs(bill.amount)}</p>
          <p><span class="label">Issued Date:</span> ${formatReadingDate(bill.issuedDate)}</p>
          <p><span class="label">Due Date:</span> ${formatReadingDate(bill.dueDate)}</p>
          <p><span class="label">Status:</span> <span class="badge ${badgeClass}">${bill.status}</span></p>
          <div class="bill-actions">${action}</div>
        </div>`;
    })
    .join("");

  billCard.innerHTML = summary + items;

  renderPaymentPage(currentBill);
}

// Formats a date consistently as "DD/MM/YYYY".
// Used by the Current Bill, Dashboard, Bill Payment, Water Usage History and Payment History pages.
function formatReadingDate(readingDate) {
  const d = new Date(readingDate);
  if (isNaN(d.getTime())) return readingDate;

  const day = String(d.getDate()).padStart(2, "0");
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const year = d.getFullYear();
  return `${day}/${month}/${year}`;
}

// Formats units consumed with the m³ unit suffix.
// Used only for the Water Usage History page.
function formatUnitsConsumed(unitsConsumed) {
  if (unitsConsumed === null || unitsConsumed === undefined || unitsConsumed === "") {
    return unitsConsumed;
  }
  return `${unitsConsumed} m³`;
}

// Bills of the logged-in customer keyed by Bill ID - used by the Water Usage History
// page for the Bill Status column and the "Pay Bill" button.
let usageBillsById = new Map();

async function loadWaterUsage(customerId) {
  const tbody = document.getElementById("usageTableBody");
  const dashUsage = document.getElementById("dashUsage");
  const dashUsagePeriod = document.getElementById("dashUsagePeriod");
  const dashViewUsageBtn = document.getElementById("dashViewUsageBtn");

  try {
    const usageList = await apiRequest(`/waterusage/customer/${customerId}`);

    // Bill status (Paid / Unpaid / Overdue) comes from the Bill API, matched by Bill ID.
    usageBillsById = new Map();
    try {
      const bills = await apiRequest(`/bill/customer/${customerId}`);
      bills.forEach((b) => usageBillsById.set(b.billId, b));
    } catch {
      // No bills for this customer yet — the status column just shows "—".
    }

    // Only the oldest outstanding bill can be paid first (see PaymentService on the API).
    const oldestUnpaid = sortOutstanding([...usageBillsById.values()])[0];

    tbody.innerHTML = usageList
      .map((u) => {
        const bill = u.billId != null ? usageBillsById.get(u.billId) : null;
        const statusCell = bill
          ? `<span class="badge ${bill.status.toLowerCase()}">${bill.status}</span>`
          : "—";
        // Only Unpaid and Overdue bills can be paid - oldest one first.
        let actionCell = "—";
        if (bill && !bill.isPaid) {
          actionCell =
            bill.billId === oldestUnpaid?.billId
              ? `<button type="button" class="btn-primary btn-sm" data-pay-bill="${bill.billId}">Pay Bill</button>`
              : `<button type="button" class="btn-primary btn-sm" disabled title="Pay your older bill first (Bill ${oldestUnpaid.billId})">Pay Bill</button>`;
        }

        return `
        <tr>
          <td>${u.billId != null ? u.billId : "—"}</td>
          <td>${formatBillingPeriod(u.billingPeriod)}</td>
          <td>${formatUnitsConsumed(u.unitsConsumed)}</td>
          <td>${formatReadingDate(u.readingDate)}</td>
          <td>${statusCell}</td>
          <td>${actionCell}</td>
        </tr>`;
      })
      .join("");

    dashUsage.textContent = `${usageList[0].unitsConsumed} m³`;
    dashUsagePeriod.textContent = usageList[0].billingPeriod;
    dashViewUsageBtn.classList.remove("hidden");
  } catch {
    tbody.innerHTML = `<tr><td colspan="6" class="section-hint">No water usage records for this customer.</td></tr>`;
    dashUsage.textContent = "No usage records";
    dashUsagePeriod.textContent = "";
    dashViewUsageBtn.classList.add("hidden");
  }
}

// "Pay Bill" on the My Bills and Water Usage History pages: open Bill Payment with that bill selected.
document.addEventListener("click", (e) => {
  const btn = e.target.closest("[data-pay-bill]");
  if (!btn) return;

  selectedPayBillId = Number(btn.dataset.payBill);
  document.querySelector('#customerPortal .tab-btn[data-tab="payment"]').click();
});

async function loadPaymentHistory(customerId) {
  const tbody = document.getElementById("paymentHistoryTableBody");

  try {
    const payments = await apiRequest(`/payment/customer/${customerId}`);

    if (!payments.length) {
      tbody.innerHTML = `<tr><td colspan="6" class="section-hint">No payments made yet.</td></tr>`;
      return;
    }

    tbody.innerHTML = [...payments]
      .sort((a, b) => a.billId - b.billId)
      .map(
        (p) => `
        <tr>
          <td>${p.paymentId}</td>
          <td>${p.billId}</td>
          <td>${formatCurrencyRs(p.amountPaid)}</td>
          <td>${formatReadingDate(p.paymentDate)}</td>
          <td>${p.paymentMethod}</td>
          <td>${p.paidByPartnerId || "-"}</td>
        </tr>`
      )
      .join("");
  } catch {
    tbody.innerHTML = `<tr><td colspan="6" class="section-hint">No payments made yet.</td></tr>`;
  }
}

// ============================================================
// SUPPORT  (Customer: Submit Support Request / View My Support Requests)
// ============================================================

async function loadSupportRequests(customerId) {
  const tbody = document.getElementById("supportRequestsTableBody");

  try {
    const requests = await apiRequest(`/supportrequest/customer/${customerId}`);

    if (!requests.length) {
      tbody.innerHTML = `<tr><td colspan="3" class="section-hint">You have not submitted any support requests yet.</td></tr>`;
      return;
    }

    tbody.innerHTML = requests
      .map((r) => {
        const badgeClass = r.status.toLowerCase().replace(/\s+/g, "");
        return `
        <tr>
          <td>${escapeAttr(r.subject)}</td>
          <td>${formatReadingDate(r.createdDate)}</td>
          <td><span class="badge ${badgeClass}">${r.status}</span></td>
        </tr>`;
      })
      .join("");
  } catch {
    tbody.innerHTML = `<tr><td colspan="3" class="section-hint">You have not submitted any support requests yet.</td></tr>`;
  }
}

document.getElementById("supportForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const subjectInput = document.getElementById("supportSubject");
  const messageInput = document.getElementById("supportMessageText");
  const subject = subjectInput.value.trim();
  const message = messageInput.value.trim();

  if (!currentCustomerId) {
    showMessage("supportMessage", "You are not logged in.", "error");
    return;
  }
  if (!subject) {
    showMessage("supportMessage", "Please enter a subject.", "error");
    return;
  }
  if (!message) {
    showMessage("supportMessage", "Please enter a message.", "error");
    return;
  }

  const submitBtn = e.target.querySelector('button[type="submit"]');
  submitBtn.disabled = true;
  submitBtn.textContent = "Submitting...";

  try {
    await apiRequest("/supportrequest", {
      method: "POST",
      body: JSON.stringify({ subject, message }),
    });

    e.target.reset();
    showMessage("supportMessage", "Your support request has been submitted.", "success");
    await loadSupportRequests(currentCustomerId);
  } catch (err) {
    showMessage("supportMessage", err.message, "error");
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = "Submit Request";
  }
});

// ============================================================
// BILL PAYMENT  (SIMULATED payment - academic/demo project)
// ============================================================
// No real payment gateway is used. The card number, expiry date and CVV are only
// checked here and by the API for demo validation; the API never stores them (only the
// payment method "Credit Card" / "Debit Card" and the amount are saved). Bill details,
// the amount and the Payment ID all come from the API - nothing is hard-coded.

// While true, the "Payment Successful!" receipt stays on screen (until the customer
// leaves the Bill Payment tab), even though the bill is no longer outstanding.
let paymentReceiptShown = false;

function showPaymentView(view) { // "form" | "empty" | "success"
  document.getElementById("paymentFormView").classList.toggle("hidden", view !== "form");
  document.getElementById("paymentEmptyView").classList.toggle("hidden", view !== "empty");
  document.getElementById("paymentSuccessView").classList.toggle("hidden", view !== "success");
}

function showPaymentError(text) {
  const box = document.getElementById("paymentMessage");
  box.textContent = text;
  box.classList.remove("hidden");
}

function hidePaymentError() {
  document.getElementById("paymentMessage").classList.add("hidden");
}

// Fills the Bill Payment page from the customer's current (outstanding) bill.
function renderPaymentPage(bill) {
  if (paymentReceiptShown) return;

  // Bill dropdown: every outstanding bill (oldest first); the bill being shown is selected.
  const select = document.getElementById("payBillSelect");
  select.innerHTML = outstandingBills
    .map(
      (b) =>
        `<option value="${b.billId}">Bill ${b.billId} — ${formatBillingPeriod(b.billingPeriod)} — ${formatCurrencyRs(b.amount)} (${b.status})</option>`
    )
    .join("");
  select.value = String(bill.billId);

  document.getElementById("payBillId").textContent = bill.billId;
  document.getElementById("payBillPeriod").textContent = formatBillingPeriod(bill.billingPeriod);
  document.getElementById("payBillAmount").textContent = formatCurrencyRs(bill.amount);
  document.getElementById("payBillDueDate").textContent = formatReadingDate(bill.dueDate);

  const statusBadge = document.getElementById("payBillStatus");
  statusBadge.textContent = bill.status.toUpperCase();
  statusBadge.className = "badge " + bill.status.toLowerCase();

  // Oldest-first rule: a newer bill can be viewed here but not paid until older ones are.
  const canPay = isOldestOutstanding(bill);
  const note = document.getElementById("payOrderNote");
  if (canPay) {
    note.classList.add("hidden");
  } else {
    const oldest = outstandingBills[0];
    note.textContent = `Bills must be paid oldest first. Please pay Bill ${oldest.billId} (${formatBillingPeriod(oldest.billingPeriod)}) before this one.`;
    note.classList.remove("hidden");
  }

  const payBtn = document.getElementById("payBtn");
  payBtn.textContent = `Pay ${formatCurrencyRs(bill.amount)}`;
  payBtn.disabled = !canPay;
  showPaymentView("form");
}

// Choosing another bill in the dropdown re-renders the Bill Payment page for that bill.
document.getElementById("payBillSelect").addEventListener("change", (e) => {
  hidePaymentError();
  selectedPayBillId = Number(e.target.value);
  const bill = outstandingBills.find((b) => b.billId === selectedPayBillId);
  if (bill) {
    currentBill = bill;
    renderPaymentPage(bill);
  }
});

// No outstanding bill: either everything is already paid, or nothing was ever billed.
async function renderPaymentUnavailable(customerId) {
  let text = "No outstanding bill to pay for this customer.";
  try {
    const bills = await apiRequest(`/bill/customer/${customerId}`);
    if (bills.length) text = "This bill has already been paid. You have no outstanding bills.";
  } catch {
    // No bills at all for this customer — keep the default text.
  }

  if (paymentReceiptShown) return;
  document.getElementById("paymentEmptyText").textContent = text;
  showPaymentView("empty");
}

// Leaving the Bill Payment tab dismisses the receipt.
function clearPaymentReceipt() {
  if (!paymentReceiptShown) return;
  paymentReceiptShown = false;
  document.getElementById("paymentEmptyText").textContent = "Loading...";
  showPaymentView("empty");
}

// Clears everything typed into the payment form and any receipt (used on logout).
function resetPaymentPage() {
  paymentReceiptShown = false;
  document.getElementById("paymentForm").reset();
  hidePaymentError();
  document.getElementById("paymentEmptyText").textContent = "Loading...";
  showPaymentView("empty");
}

// Called by the tab handler each time a Customer Portal tab is opened.
function onCustomerTabOpened(tab) {
  if (tab !== "payment") {
    clearPaymentReceipt();
    selectedPayBillId = null;
  }
  if (!currentCustomerId) return;

  if (tab === "dashboard" || tab === "bill" || tab === "payment") loadCurrentBill(currentCustomerId);
  if (tab === "dashboard" || tab === "usage") loadWaterUsage(currentCustomerId);
  if (tab === "paymentHistory") loadPaymentHistory(currentCustomerId);
  if (tab === "support") loadSupportRequests(currentCustomerId);
}

// Input helpers: keep the fields tidy while the customer types.
document.getElementById("cardNumber").addEventListener("input", (e) => {
  const digits = e.target.value.replace(/\D/g, "").slice(0, 19);
  e.target.value = digits.replace(/(.{4})/g, "$1 ").trim();
});

document.getElementById("cardExpiry").addEventListener("input", (e) => {
  const digits = e.target.value.replace(/\D/g, "").slice(0, 4);
  e.target.value = digits.length > 2 ? `${digits.slice(0, 2)} / ${digits.slice(2)}` : digits;
});

document.getElementById("cardCvv").addEventListener("input", (e) => {
  e.target.value = e.target.value.replace(/\D/g, "").slice(0, 4);
});

// Returns { error } or { method, cardNumber, expiryDate, cvv } (already cleaned up).
function validatePaymentForm() {
  const method = document.querySelector('#paymentForm input[name="paymentMethod"]:checked')?.value;
  const cardNumber = document.getElementById("cardNumber").value.replace(/[\s-]/g, "");
  const expiry = document.getElementById("cardExpiry").value.trim();
  const cvv = document.getElementById("cardCvv").value.trim();

  if (!method) return { error: "Please select a payment method (Credit Card or Debit Card)." };

  if (!cardNumber) return { error: "Please enter your card number." };
  if (!/^\d{13,19}$/.test(cardNumber)) return { error: "Card number must contain 13 to 19 digits." };

  if (!expiry) return { error: "Please enter the expiry date." };
  const match = expiry.match(/^(0[1-9]|1[0-2])\s*\/\s*(\d{2})$/);
  if (!match) return { error: "Expiry date must be in MM / YY format." };
  const now = new Date();
  const expYear = 2000 + Number(match[2]);
  const expMonth = Number(match[1]);
  if (expYear < now.getFullYear() || (expYear === now.getFullYear() && expMonth < now.getMonth() + 1)) {
    return { error: "This card has expired." };
  }

  if (!cvv) return { error: "Please enter the CVV." };
  if (!/^\d{3,4}$/.test(cvv)) return { error: "CVV must be 3 or 4 digits." };

  return { method, cardNumber, expiryDate: `${match[1]}/${match[2]}`, cvv };
}

document.getElementById("paymentForm").addEventListener("submit", async (e) => {
  e.preventDefault();
  hidePaymentError();

  if (!currentCustomerId) {
    showPaymentError("You are not logged in.");
    return;
  }
  if (!currentBill) {
    showPaymentError("There is no outstanding bill to pay for this customer.");
    return;
  }
  if (!isOldestOutstanding(currentBill)) {
    showPaymentError("Bills must be paid oldest first. Please pay your oldest outstanding bill before this one.");
    return;
  }

  // Validation failed -> clear message, nothing is sent, no payment record, bill unchanged.
  const form = validatePaymentForm();
  if (form.error) {
    showPaymentError(form.error);
    return;
  }

  const payBtn = document.getElementById("payBtn");
  payBtn.disabled = true;
  payBtn.textContent = "Processing...";

  try {
    // The API validates again, creates the Payment record and marks the bill as PAID.
    // The amount is not sent: the API charges the bill's own amount.
    const payment = await apiRequest("/payment/simulate", {
      method: "POST",
      body: JSON.stringify({
        customerId: currentCustomerId,
        billId: currentBill.billId,
        paymentMethod: form.method,
        cardNumber: form.cardNumber,
        expiryDate: form.expiryDate,
        cvv: form.cvv,
      }),
    });

    // "Payment Successful!" — every value below comes from the API's response.
    document.getElementById("rcptPaymentId").textContent = payment.paymentId;
    document.getElementById("rcptBillId").textContent = payment.billId;
    document.getElementById("rcptAmount").textContent = formatCurrencyRs(payment.amountPaid);
    document.getElementById("rcptMethod").textContent = payment.paymentMethod;
    document.getElementById("rcptPaidBy").textContent = payment.paidByPartnerId || "-";
    paymentReceiptShown = true;
    showPaymentView("success");

    // Card details are only needed for this one request — clear them straight away.
    document.getElementById("paymentForm").reset();

    // Re-read the bill (now PAID) and the payment list from the backend.
    selectedPayBillId = null;
    await Promise.all([loadCurrentBill(currentCustomerId), loadPaymentHistory(currentCustomerId), loadWaterUsage(currentCustomerId)]);
  } catch (err) {
    if (/already been paid/i.test(err.message)) {
      // The bill was paid elsewhere (e.g. through a partner) — refresh and show that state.
      selectedPayBillId = null;
      await Promise.all([loadCurrentBill(currentCustomerId), loadPaymentHistory(currentCustomerId), loadWaterUsage(currentCustomerId)]);
    } else {
      showPaymentError(err.message);
    }
  } finally {
    payBtn.disabled = currentBill ? !isOldestOutstanding(currentBill) : false;
    payBtn.textContent = currentBill ? `Pay ${formatCurrencyRs(currentBill.amount)}` : "Pay";
  }
});

// ============================================================
// STAFF PORTAL
// ============================================================

// -- Register New Partner --
// Partner ID and API key are both generated by the backend - Staff only supplies the name
// and contact email. The API key is shown once, on the success view below; it is never
// retrievable again after that (see PartnerController.cs on the backend).
const PARTNER_FIELDS = {
  name: ["newPartnerName", "partnerNameError"],
  email: ["newPartnerEmail", "partnerEmailError"],
  phone: ["newPartnerPhone", "partnerPhoneError"],
  type: ["newPartnerType", "partnerTypeError"],
};

function setPartnerFieldError(field, message) {
  const [inputId, errorId] = PARTNER_FIELDS[field];
  const input = document.getElementById(inputId);
  document.getElementById(errorId).textContent = message || "";
  input.classList.toggle("input-invalid", Boolean(message));
  input.setAttribute("aria-invalid", message ? "true" : "false");
}

function clearPartnerErrors() {
  Object.keys(PARTNER_FIELDS).forEach((field) => setPartnerFieldError(field, ""));
}

Object.entries(PARTNER_FIELDS).forEach(([field, [inputId]]) => {
  document.getElementById(inputId).addEventListener("input", () => setPartnerFieldError(field, ""));
});

function validatePartnerForm(values) {
  const errors = {};
  if (!values.name) errors.name = "Partner Name is required.";
  if (!values.email) {
    errors.email = "Contact Email is required.";
  } else if (!EMAIL_REGEX.test(values.email)) {
    errors.email = "Please enter a valid email address.";
  }
  if (!values.phone) errors.phone = "Phone Number is required.";
  if (!values.type) errors.type = "Partner Type is required.";
  return errors;
}

function showPartnerRegisterForm() {
  document.getElementById("staffPartnerFormView").classList.remove("hidden");
  document.getElementById("staffPartnerSuccessView").classList.add("hidden");
  clearPartnerErrors();
  document.getElementById("staffPartnerForm").reset();
  document.getElementById("staffMessageBox").classList.add("hidden");
}

document.getElementById("staffPartnerForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const payload = {
    partnerName: document.getElementById("newPartnerName").value.trim(),
    contactEmail: document.getElementById("newPartnerEmail").value.trim(),
    phoneNumber: document.getElementById("newPartnerPhone").value.trim(),
    partnerType: document.getElementById("newPartnerType").value,
    businessAddress: document.getElementById("newPartnerAddress").value.trim(),
    businessRegistrationNo: document.getElementById("newPartnerRegNo").value.trim(),
    description: document.getElementById("newPartnerDescription").value.trim(),
  };

  const errors = validatePartnerForm({
    name: payload.partnerName,
    email: payload.contactEmail,
    phone: payload.phoneNumber,
    type: payload.partnerType,
  });
  clearPartnerErrors();
  const invalidFields = Object.keys(PARTNER_FIELDS).filter((field) => errors[field]);
  if (invalidFields.length) {
    invalidFields.forEach((field) => setPartnerFieldError(field, errors[field]));
    document.getElementById(PARTNER_FIELDS[invalidFields[0]][0]).focus();
    return;
  }

  try {
    const created = await apiRequest("/partner", { method: "POST", body: JSON.stringify(payload) });

    document.getElementById("newPartnerIdResult").textContent = created.partnerId;
    document.getElementById("newPartnerApiKeyResult").textContent = created.apiKey;
    document.getElementById("copyPartnerApiKeyBtn").textContent = "Copy API Key";

    document.getElementById("staffPartnerFormView").classList.add("hidden");
    document.getElementById("staffPartnerSuccessView").classList.remove("hidden");
    document.getElementById("staffMessageBox").classList.add("hidden");
  } catch (err) {
    showMessage("staffMessageBox", err.message, "error");
  }
});

document.getElementById("copyPartnerApiKeyBtn").addEventListener("click", async () => {
  const btn = document.getElementById("copyPartnerApiKeyBtn");
  const apiKey = document.getElementById("newPartnerApiKeyResult").textContent;
  try {
    await navigator.clipboard.writeText(apiKey);
    btn.textContent = "Copied!";
  } catch {
    btn.textContent = "Couldn't copy - select and copy manually";
  }
  setTimeout(() => { btn.textContent = "Copy API Key"; }, 2000);
});

document.getElementById("registerAnotherPartnerBtn").addEventListener("click", showPartnerRegisterForm);

// -- Generate Bill --
// Staff picks a Customer ID and one of that customer's already-recorded meter-reading
// billing periods that hasn't been billed yet - the amount, due date and Bill ID are all
// calculated by the backend (see BillService.GenerateBillAsync), never typed in here.

function showBillGenerateForm() {
  document.getElementById("staffBillFormView").classList.remove("hidden");
  document.getElementById("staffBillSuccessView").classList.add("hidden");
  document.getElementById("staffBillForm").reset();
  resetBillPeriodSelect("Load billing periods first...");
  document.getElementById("staffMessageBox").classList.add("hidden");
}

function resetBillPeriodSelect(placeholder) {
  const select = document.getElementById("billPeriod");
  select.innerHTML = `<option value="" selected disabled>${placeholder}</option>`;
}

document.getElementById("loadBillPeriodsBtn").addEventListener("click", async () => {
  const customerId = document.getElementById("billCustomerId").value.trim();
  const hint = document.getElementById("billPeriodHint");

  if (!customerId) {
    showMessage("staffMessageBox", "Enter a Customer ID first.", "error");
    return;
  }

  try {
    // The backend computes this directly from the customer's actual WaterUsage + Bill
    // records (WaterUsageService.GetUnbilledUsageAsync) - a reading only appears here if
    // it has no Bill yet for the same Customer ID + Billing Period. Nothing is filtered or
    // hard-coded on the frontend; a 404 here means the customer genuinely has none.
    let available = [];
    try {
      available = await apiRequest(`/waterusage/customer/${customerId}/unbilled`);
    } catch {
      available = [];
    }

    if (!available.length) {
      resetBillPeriodSelect("No unbilled meter readings for this customer");
      hint.textContent = "Every recorded meter reading for this customer already has a bill.";
      return;
    }

    const select = document.getElementById("billPeriod");
    select.innerHTML =
      `<option value="" selected disabled>Choose a billing period...</option>` +
      available
        .map(
          (u) =>
            `<option value="${u.billingPeriod}">${formatBillingPeriod(u.billingPeriod)} (${formatUnitsConsumed(u.unitsConsumed)})</option>`
        )
        .join("");
    hint.textContent = "Choose the billing period to generate a bill for.";
  } catch (err) {
    resetBillPeriodSelect("Load billing periods first...");
    showMessage("staffMessageBox", err.message, "error");
  }
});

document.getElementById("staffBillForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const payload = {
    customerId: document.getElementById("billCustomerId").value.trim(),
    billingPeriod: document.getElementById("billPeriod").value,
  };

  if (!payload.customerId || !payload.billingPeriod) {
    showMessage("staffMessageBox", "Choose a Customer ID and a billing period.", "error");
    return;
  }

  try {
    const bill = await apiRequest("/bill", { method: "POST", body: JSON.stringify(payload) });

    document.getElementById("genBillId").textContent = bill.billId;
    document.getElementById("genBillCustomerId").textContent = bill.customerId;
    document.getElementById("genBillPeriod").textContent = formatBillingPeriod(bill.billingPeriod);
    document.getElementById("genBillUsage").textContent =
      bill.unitsConsumed != null ? formatUnitsConsumed(bill.unitsConsumed) : "Not available";
    document.getElementById("genBillAmount").textContent = formatCurrencyRs(bill.amount);
    document.getElementById("genBillIssuedDate").textContent = formatReadingDate(bill.issuedDate);
    document.getElementById("genBillDueDate").textContent = formatReadingDate(bill.dueDate);
    document.getElementById("genBillStatus").textContent = bill.status;

    document.getElementById("staffBillFormView").classList.add("hidden");
    document.getElementById("staffBillSuccessView").classList.remove("hidden");
    document.getElementById("staffMessageBox").classList.add("hidden");
  } catch (err) {
    showMessage("staffMessageBox", err.message, "error");
  }
});

document.getElementById("generateAnotherBillBtn").addEventListener("click", showBillGenerateForm);

// -- Record Meter Reading (Water Usage) --
// Customer ID is validated against the existing Customer API (GET /customer/{id}) before
// anything is recorded. Negative units, missing fields and duplicate readings for the same
// Customer ID + Billing Period are all rejected by the backend (see
// WaterUsageService.RecordUsageAsync) - this form never stores the reading in frontend-only
// state, it only reflects what the Meter Reading API confirms was saved.
const USAGE_FIELDS = {
  customerId: ["usageCustomerId", "usageCustomerIdError"],
  billingPeriod: ["usagePeriod", "usagePeriodError"],
  unitsConsumed: ["usageUnits", "usageUnitsError"],
  readingDate: ["usageDate", "usageDateError"],
};

function setUsageFieldError(field, message) {
  const [inputId, errorId] = USAGE_FIELDS[field];
  const input = document.getElementById(inputId);
  document.getElementById(errorId).textContent = message || "";
  input.classList.toggle("input-invalid", Boolean(message));
  input.setAttribute("aria-invalid", message ? "true" : "false");
}

function clearUsageErrors() {
  Object.keys(USAGE_FIELDS).forEach((field) => setUsageFieldError(field, ""));
}

Object.entries(USAGE_FIELDS).forEach(([field, [inputId]]) => {
  document.getElementById(inputId).addEventListener("input", () => setUsageFieldError(field, ""));
});

function validateUsageForm(values) {
  const errors = {};
  if (!values.customerId) errors.customerId = "Customer ID is required.";
  if (!values.billingPeriod) errors.billingPeriod = "Billing Period is required.";

  if (values.unitsConsumedRaw === "") {
    errors.unitsConsumed = "Units Consumed is required.";
  } else if (Number.isNaN(values.unitsConsumed) || values.unitsConsumed < 0) {
    errors.unitsConsumed = "Units Consumed must be a number of 0 or more.";
  }

  if (!values.readingDate) errors.readingDate = "Reading Date is required.";

  return errors;
}

function showUsageRecordForm() {
  document.getElementById("staffUsageFormView").classList.remove("hidden");
  document.getElementById("staffUsageSuccessView").classList.add("hidden");
  clearUsageErrors();
  document.getElementById("staffUsageForm").reset();
  document.getElementById("staffMessageBox").classList.add("hidden");
}

document.getElementById("staffUsageForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const unitsRaw = document.getElementById("usageUnits").value;
  const values = {
    customerId: document.getElementById("usageCustomerId").value.trim(),
    billingPeriod: document.getElementById("usagePeriod").value,
    unitsConsumedRaw: unitsRaw,
    unitsConsumed: parseFloat(unitsRaw),
    readingDate: document.getElementById("usageDate").value,
  };

  const errors = validateUsageForm(values);
  clearUsageErrors();
  const invalidFields = Object.keys(USAGE_FIELDS).filter((field) => errors[field]);
  if (invalidFields.length) {
    invalidFields.forEach((field) => setUsageFieldError(field, errors[field]));
    document.getElementById(USAGE_FIELDS[invalidFields[0]][0]).focus();
    return;
  }

  document.getElementById("staffMessageBox").classList.add("hidden");

  // Validate the Customer ID against the existing Customer API before recording anything.
  try {
    await apiRequest(`/customer/${values.customerId}`);
  } catch {
    setUsageFieldError("customerId", "Customer not found.");
    document.getElementById("usageCustomerId").focus();
    return;
  }

  const payload = {
    customerId: values.customerId,
    billingPeriod: values.billingPeriod,
    unitsConsumed: values.unitsConsumed,
    readingDate: values.readingDate,
  };

  try {
    // The Meter Reading API (WaterUsageService.RecordUsageAsync) does its own duplicate and
    // negative-units checks server-side too, so this record is only ever created there.
    const created = await apiRequest("/waterusage", { method: "POST", body: JSON.stringify(payload) });

    document.getElementById("recUsageCustomerId").textContent = created.customerId;
    document.getElementById("recUsagePeriod").textContent = formatBillingPeriod(created.billingPeriod);
    document.getElementById("recUsageUnits").textContent = formatUnitsConsumed(created.unitsConsumed);
    document.getElementById("recUsageDate").textContent = formatReadingDate(created.readingDate);

    document.getElementById("staffUsageFormView").classList.add("hidden");
    document.getElementById("staffUsageSuccessView").classList.remove("hidden");
  } catch (err) {
    showMessage("staffMessageBox", err.message, "error");
  }
});

document.getElementById("recordAnotherUsageBtn").addEventListener("click", showUsageRecordForm);

// -- View Payment Details (optionally filtered by a single Customer ID) --
async function loadStaffPayments(customerId) {
  const tbody = document.getElementById("staffPaymentsTableBody");
  const clearBtn = document.getElementById("staffClearPaymentsFilterBtn");
  try {
    const payments = customerId
      ? await apiRequest(`/payment/customer/${encodeURIComponent(customerId)}`)
      : await apiRequest("/payment");

    clearBtn.classList.toggle("hidden", !customerId);

    if (!payments.length) {
      tbody.innerHTML = `<tr><td colspan="8" class="section-hint">${
        customerId ? `No payments found for customer ${customerId}.` : "No payment records found."
      }</td></tr>`;
      return;
    }

    tbody.innerHTML = payments
      .map(
        (p) => `
        <tr>
          <td>${p.paymentId}</td>
          <td>${p.customerId}</td>
          <td>${p.billId}</td>
          <td>${formatCurrencyRs(p.amountPaid)}</td>
          <td>${p.paymentMethod}</td>
          <td>${formatReadingDate(p.paymentDate)}</td>
          <td>${p.paidByPartnerId ?? "-"}</td>
          <td><span class="badge paid">${p.status}</span></td>
        </tr>`
      )
      .join("");
  } catch (err) {
    // A 404 for "no bills/payments for this customer" surfaces as an error from apiRequest;
    // treat it as an empty result instead of showing a scary error banner.
    if (customerId && /not found|404/i.test(err.message)) {
      clearBtn.classList.remove("hidden");
      tbody.innerHTML = `<tr><td colspan="8" class="section-hint">No payments found for customer ${customerId}.</td></tr>`;
      return;
    }
    showMessage("staffMessageBox", err.message, "error");
  }
}

document.getElementById("staffLoadPaymentsBtn").addEventListener("click", () => {
  const customerId = document.getElementById("staffPaymentsCustomerId").value.trim();
  loadStaffPayments(customerId);
});

document.getElementById("staffClearPaymentsFilterBtn").addEventListener("click", () => {
  document.getElementById("staffPaymentsCustomerId").value = "";
  loadStaffPayments("");
});

// -- Manage Bill Information (View All Bills across every customer) --
document.getElementById("staffLoadBillsBtn").addEventListener("click", async () => {
  const tbody = document.getElementById("staffBillsTableBody");
  try {
    const bills = await apiRequest("/bill");

    if (!bills.length) {
      tbody.innerHTML = `<tr><td colspan="7" class="section-hint">No bills found.</td></tr>`;
      return;
    }

    tbody.innerHTML = bills
      .map(
        (b) => `
        <tr>
          <td>${b.billId}</td>
          <td>${b.customerId}</td>
          <td>${b.billingPeriod}</td>
          <td>${formatReadingDate(b.issuedDate)}</td>
          <td>${formatReadingDate(b.dueDate)}</td>
          <td>${formatCurrencyRs(b.amount)}</td>
          <td><span class="badge ${b.status.toLowerCase()}">${b.status}</span></td>
        </tr>`
      )
      .join("");
  } catch (err) {
    showMessage("staffMessageBox", err.message, "error");
  }
});

// ============================================================
// STAFF: SUPPORT REQUESTS  (View All Support Requests / Update Support Request)
// ============================================================

let staffSupportRequests = []; // last-loaded list, so "Open" doesn't need a second request

function showStaffSupportView(view) { // "list" | "detail"
  document.getElementById("staffSupportListView").classList.toggle("hidden", view !== "list");
  document.getElementById("staffSupportDetailView").classList.toggle("hidden", view !== "detail");
}

document.getElementById("staffLoadSupportBtn").addEventListener("click", async () => {
  const tbody = document.getElementById("staffSupportTableBody");
  try {
    staffSupportRequests = await apiRequest("/supportrequest");

    if (!staffSupportRequests.length) {
      tbody.innerHTML = `<tr><td colspan="5" class="section-hint">No support requests found.</td></tr>`;
      return;
    }

    tbody.innerHTML = staffSupportRequests
      .map((r) => {
        const badgeClass = r.status.toLowerCase().replace(/\s+/g, "");
        return `
        <tr>
          <td>${r.customerId}</td>
          <td>${escapeAttr(r.subject)}</td>
          <td>${formatReadingDate(r.createdDate)}</td>
          <td><span class="badge ${badgeClass}">${r.status}</span></td>
          <td><button type="button" class="btn-outline btn-sm" data-open-support="${r.supportRequestId}">Open</button></td>
        </tr>`;
      })
      .join("");
  } catch (err) {
    showMessage("staffMessageBox", err.message, "error");
  }
});

document.getElementById("staffSupportTableBody").addEventListener("click", (e) => {
  const btn = e.target.closest("[data-open-support]");
  if (!btn) return;

  const id = Number(btn.dataset.openSupport);
  const request = staffSupportRequests.find((r) => r.supportRequestId === id);
  if (!request) return;

  document.getElementById("staffSupportUpdateForm").dataset.requestId = request.supportRequestId;
  document.getElementById("staffSupportCustomerId").textContent = request.customerId;
  document.getElementById("staffSupportSubject").textContent = request.subject;
  document.getElementById("staffSupportDate").textContent = formatReadingDate(request.createdDate);
  document.getElementById("staffSupportMessageText").textContent = request.message;
  document.getElementById("staffSupportStatus").value = request.status;
  document.getElementById("staffSupportResponse").value = request.staffResponse || "";

  document.getElementById("staffMessageBox").classList.add("hidden");
  showStaffSupportView("detail");
});

document.getElementById("staffSupportBackBtn").addEventListener("click", () => {
  showStaffSupportView("list");
});

document.getElementById("staffSupportUpdateForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const id = Number(e.target.dataset.requestId);
  const status = document.getElementById("staffSupportStatus").value;
  const staffResponse = document.getElementById("staffSupportResponse").value.trim();

  const saveBtn = e.target.querySelector('button[type="submit"]');
  saveBtn.disabled = true;
  saveBtn.textContent = "Saving...";

  try {
    const updated = await apiRequest(`/supportrequest/${id}`, {
      method: "PUT",
      body: JSON.stringify({ status, staffResponse }),
    });

    // Keep the loaded list in sync so re-opening the same request shows the new values.
    const index = staffSupportRequests.findIndex((r) => r.supportRequestId === id);
    if (index !== -1) staffSupportRequests[index] = updated;

    showMessage("staffMessageBox", `Support request ${id} updated successfully.`, "success");
    showStaffSupportView("list");
    document.getElementById("staffLoadSupportBtn").click();
  } catch (err) {
    showMessage("staffMessageBox", err.message, "error");
  } finally {
    saveBtn.disabled = false;
    saveBtn.textContent = "Save";
  }
});

// ============================================================
// STAFF: CONTACT MESSAGES  (View Contact Messages / Update Contact Message Status)
// ============================================================

let staffContactMessages = []; // last-loaded list, so "Open" doesn't need a second request

function showStaffContactView(view) { // "list" | "detail"
  document.getElementById("staffContactListView").classList.toggle("hidden", view !== "list");
  document.getElementById("staffContactDetailView").classList.toggle("hidden", view !== "detail");
}

// "InProgress" (server value) -> "inprogress" (CSS badge class); "New"/"Resolved" already match.
function contactStatusBadgeClass(status) {
  return status.toLowerCase().replace(/\s+/g, "");
}

document.getElementById("staffLoadContactBtn").addEventListener("click", async () => {
  const tbody = document.getElementById("staffContactTableBody");
  try {
    staffContactMessages = await apiRequest("/contact");

    if (!staffContactMessages.length) {
      tbody.innerHTML = `<tr><td colspan="6" class="section-hint">No contact messages found.</td></tr>`;
      return;
    }

    tbody.innerHTML = staffContactMessages
      .map((c) => {
        const badgeClass = contactStatusBadgeClass(c.status);
        return `
        <tr>
          <td>${escapeAttr(c.name)}</td>
          <td>${escapeAttr(c.email)}</td>
          <td>${escapeAttr(c.subject)}</td>
          <td>${formatReadingDate(c.createdAt)}</td>
          <td><span class="badge ${badgeClass}">${c.status}</span></td>
          <td><button type="button" class="btn-outline btn-sm" data-open-contact="${c.id}">Open</button></td>
        </tr>`;
      })
      .join("");
  } catch (err) {
    showMessage("staffMessageBox", err.message, "error");
  }
});

document.getElementById("staffContactTableBody").addEventListener("click", (e) => {
  const btn = e.target.closest("[data-open-contact]");
  if (!btn) return;

  const id = Number(btn.dataset.openContact);
  const message = staffContactMessages.find((c) => c.id === id);
  if (!message) return;

  document.getElementById("staffContactUpdateForm").dataset.messageId = message.id;
  document.getElementById("staffContactName").textContent = message.name;
  document.getElementById("staffContactEmail").textContent = message.email;
  document.getElementById("staffContactSubject").textContent = message.subject;
  document.getElementById("staffContactDate").textContent = formatReadingDate(message.createdAt);
  document.getElementById("staffContactMessageText").textContent = message.message;
  document.getElementById("staffContactStatus").value = message.status;
  document.getElementById("staffContactResponse").value = message.staffResponse || "";

  document.getElementById("staffMessageBox").classList.add("hidden");
  showStaffContactView("detail");
});

document.getElementById("staffContactBackBtn").addEventListener("click", () => {
  showStaffContactView("list");
});

document.getElementById("staffContactUpdateForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const id = Number(e.target.dataset.messageId);
  const status = document.getElementById("staffContactStatus").value;
  const staffResponse = document.getElementById("staffContactResponse").value.trim();

  const saveBtn = e.target.querySelector('button[type="submit"]');
  saveBtn.disabled = true;
  saveBtn.textContent = "Saving...";

  try {
    const updated = await apiRequest(`/contact/${id}/status`, {
      method: "PUT",
      body: JSON.stringify({ status, staffResponse }),
    });

    const index = staffContactMessages.findIndex((c) => c.id === id);
    if (index !== -1) staffContactMessages[index] = updated;

    showMessage("staffMessageBox", `Contact message ${id} updated successfully.`, "success");
    showStaffContactView("list");
    document.getElementById("staffLoadContactBtn").click();
  } catch (err) {
    showMessage("staffMessageBox", err.message, "error");
  } finally {
    saveBtn.disabled = false;
    saveBtn.textContent = "Save";
  }
});

// ============================================================
// CONTACT US  (Public: Submit Contact Message)
// ============================================================

document.getElementById("contactForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const nameInput = document.getElementById("contactName");
  const emailInput = document.getElementById("contactEmail");
  const subjectInput = document.getElementById("contactSubject");
  const messageInput = document.getElementById("contactMessage");

  const name = nameInput.value.trim();
  const email = emailInput.value.trim();
  const subject = subjectInput.value.trim();
  const message = messageInput.value.trim();

  document.getElementById("contactFormMessage").classList.add("hidden");

  if (!name || !email || !subject || !message) {
    showMessage("contactFormMessage", "Please fill in all fields.", "error");
    return;
  }
  if (!EMAIL_REGEX.test(email)) {
    showMessage("contactFormMessage", "Please enter a valid email address.", "error");
    return;
  }

  const submitBtn = document.getElementById("contactSubmitBtn");
  submitBtn.disabled = true;
  submitBtn.textContent = "Sending...";

  try {
    // The API is the only source of truth for whether this was actually saved -
    // "submitted successfully" is only ever shown once it responds without error.
    await apiRequest("/contact", {
      method: "POST",
      body: JSON.stringify({ name, email, subject, message }),
    });

    e.target.reset();
    showMessage("contactFormMessage", "Your message has been submitted successfully.", "success");
  } catch (err) {
    showMessage("contactFormMessage", err.message, "error");
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = "Send Message";
  }
});
