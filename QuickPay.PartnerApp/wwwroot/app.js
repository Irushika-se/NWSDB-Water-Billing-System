// QuickPay Lanka - web page. Talks ONLY to QuickPay's own server (same origin, /api/...).
// QuickPay's server relays every call to the NWSDB API using THIS QuickPay account's own
// JWT (issued by NWSDB's separate QuickPay login, not the customer's NWSDB login); this
// page never sees an NWSDB partner API key, and never sends a Customer ID - there is
// nowhere on this page to type one in.

const $ = (id) => document.getElementById(id);
const esc = (v) => String(v ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const rs = (n) => "Rs. " + Number(n).toFixed(2);

const SESSION_KEY = "quickpay.session"; // { token, displayName }

function getSession() {
  try { return JSON.parse(localStorage.getItem(SESSION_KEY) || "null"); } catch { return null; }
}
function setSession(session) { localStorage.setItem(SESSION_KEY, JSON.stringify(session)); }
function clearSession() { localStorage.removeItem(SESSION_KEY); }

let currentBill = null;

function togglePasswordVisibility(inputId, buttonId) {
  const input = $(inputId);
  const button = $(buttonId);
  if (!input || !button) return;
  button.addEventListener("click", () => {
    const isHidden = input.type === "password";
    input.type = isHidden ? "text" : "password";
    button.setAttribute("aria-label", isHidden ? "Hide password" : "Show password");
  });
}
togglePasswordVisibility("loginPassword", "loginPasswordToggle");

// Every call goes through here. If the session has expired (NWSDB/QuickPay both answer 401
// the same way), the customer is dropped back to the login screen - never shown someone
// else's cached data.
async function api(path, options = {}) {
  const session = getSession();
  const headers = { "Content-Type": "application/json", ...(options.headers || {}) };
  if (session?.token) headers.Authorization = "Bearer " + session.token;

  const res = await fetch(path, { ...options, headers });
  let data = null;
  try { data = await res.json(); } catch { data = null; }

  if (res.status === 401) {
    clearSession();
    showLoggedOut();
    throw new Error((data && data.message) || "Your session has expired. Please log in again.");
  }
  if (!res.ok) throw new Error((data && (data.message || data.title)) || `Request failed (${res.status})`);
  return data;
}

let messageTimer = null;
function showMessage(text, type) {
  const box = $("messageBox");
  box.textContent = text;
  box.className = "message " + type;
  clearTimeout(messageTimer);
  messageTimer = setTimeout(() => box.classList.add("hidden"), 6000);
  window.scrollTo({ top: 0, behavior: "smooth" });
}

// ---------- view switching ----------
function showLoggedOut() {
  currentBill = null;
  $("loginSection").classList.remove("hidden");
  $("dashboard").classList.add("hidden");
  $("sessionPill").classList.add("hidden");
  $("logoutBtn").classList.add("hidden");
  $("loginForm").reset();
}

function showLoggedIn(displayName) {
  $("loginSection").classList.add("hidden");
  $("dashboard").classList.remove("hidden");
  $("sessionPill").textContent = "Logged in as " + displayName;
  $("sessionPill").className = "pill pill-ok";
  $("logoutBtn").classList.remove("hidden");
}

// ---------- login ----------
$("loginForm").addEventListener("submit", async (e) => {
  e.preventDefault();
  $("loginErrors").classList.add("hidden");

  const email = $("loginEmail").value.trim();
  const password = $("loginPassword").value;
  if (!email || !password) return;

  const btn = $("loginBtn");
  btn.disabled = true;
  try {
    const auth = await api("/api/session/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    });
    setSession({ token: auth.token, displayName: auth.displayName });
    showLoggedIn(auth.displayName);
    await loadDashboard();
  } catch (err) {
    $("loginErrors").textContent = err.message;
    $("loginErrors").classList.remove("hidden");
  } finally {
    btn.disabled = false;
  }
});

$("logoutBtn").addEventListener("click", () => {
  clearSession();
  showMessage("Logged out.", "success");
  showLoggedOut();
});

// ---------- dashboard: My Current Bill ----------
async function loadDashboard() {
  $("receiptSection").classList.add("hidden");
  try {
    const d = await api("/api/me/dashboard");

    $("cId").textContent = d.customer.customerId;
    $("cName").textContent = d.customer.name;
    $("cUsage").textContent = d.latestUsage ? `${d.latestUsage.unitsConsumed} m\u00B3 (${d.latestUsage.billingPeriod})` : "No usage records";

    currentBill = d.bill;
    const b = d.bill;
    $("billBox").innerHTML = b
      ? `<p><span class="label">Bill no.</span> ${esc(b.billId)}</p>
         <p><span class="label">Billing period</span> ${esc(b.billingPeriod)}</p>
         <p><span class="label">Water usage</span> ${d.latestUsage ? esc(d.latestUsage.unitsConsumed) + " m\u00B3" : "\u2013"}</p>
         <p><span class="label">Amount</span> ${rs(b.amount)}</p>
         <p><span class="label">Due date</span> ${new Date(b.dueDate).toLocaleDateString()}</p>
         <p><span class="label">Status</span> <span class="badge ${esc(b.status.toLowerCase())}">${esc(b.status)}</span></p>`
      : `<p class="hint">No outstanding bill &mdash; everything is paid.</p>`;

    renderUnpaidBills(d.bills);

    if (b && !b.isPaid) {
      $("payAmount").textContent = rs(b.amount);
      $("paySection").classList.remove("hidden");
    } else {
      $("paySection").classList.add("hidden");
    }

    await loadPaymentHistory();
  } catch (err) {
    if (getSession()) showMessage(err.message, "error"); // suppress if we just logged out
  }
}

// ---------- Payment History ----------
async function loadPaymentHistory() {
  try {
    const payments = await api("/api/me/payments");
    const sorted = [...(payments || [])].sort((a, b) => new Date(b.paymentDate) - new Date(a.paymentDate));

    $("paymentHistoryBody").innerHTML = sorted.length
      ? sorted.map((p) => `<tr><td>${esc(p.paymentId)}</td><td>#${esc(p.billId)}</td><td>${rs(p.amountPaid)}</td><td>${esc(p.paymentMethod)}</td><td>${new Date(p.paymentDate).toLocaleDateString()}</td><td><span class="badge paid">${esc(p.status)}</span></td></tr>`).join("")
      : `<tr><td colspan="6" class="hint">No payments yet.</td></tr>`;
  } catch (err) {
    // Dashboard already loaded fine - don't block the page over payment history alone.
  }
}

// ---------- Bills to Pay: every unpaid bill, oldest first ----------
// Same ordering NWSDB uses for the "current" bill (due date, then bill id). Only the first
// row can be paid; NWSDB rejects any other bill anyway (oldest-first rule in PaymentService).
function renderUnpaidBills(bills) {
  const unpaid = (bills || [])
    .filter((x) => !x.isPaid)
    .sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate) || a.billId - b.billId);

  $("unpaidBody").innerHTML = unpaid.length
    ? unpaid.map((x, i) => `<tr><td>${i + 1}</td><td>#${esc(x.billId)}</td><td>${esc(x.billingPeriod)}</td><td>${rs(x.amount)}</td><td>${new Date(x.dueDate).toLocaleDateString()}</td><td><span class="badge ${esc(x.status.toLowerCase())}">${esc(x.status)}</span></td><td>${i === 0
        ? `<button type="button" class="btn" data-pay-bill="${esc(x.billId)}">Pay now</button>`
        : `<span class="hint">Pay oldest bill first</span>`}</td></tr>`).join("")
    : `<tr><td colspan="7" class="hint">No outstanding bills &mdash; everything is paid.</td></tr>`;
}

// "Pay now" on the oldest bill just scrolls down to the payment form for that bill.
$("unpaidBody").addEventListener("click", (e) => {
  if (e.target.closest("[data-pay-bill]")) $("paySection").scrollIntoView({ behavior: "smooth" });
});

// ---------- pay (SIMULATED - demo only, no real payment gateway) ----------
// Card details are only used for demo validation. They are sent once to QuickPay's server
// (which forwards them to NWSDB for validation), never stored, and cleared after the request.
function showCardError(text) {
  const box = $("cardErrors");
  box.textContent = text;
  box.classList.remove("hidden");
}

$("cardNumber").addEventListener("input", (e) => {
  const digits = e.target.value.replace(/\D/g, "").slice(0, 19);
  e.target.value = digits.replace(/(.{4})/g, "$1 ").trim();
});
$("cardExpiry").addEventListener("input", (e) => {
  const digits = e.target.value.replace(/\D/g, "").slice(0, 4);
  e.target.value = digits.length > 2 ? `${digits.slice(0, 2)} / ${digits.slice(2)}` : digits;
});
$("cardCvv").addEventListener("input", (e) => { e.target.value = e.target.value.replace(/\D/g, "").slice(0, 4); });

function validateCard() {
  const method = document.querySelector('#payForm input[name="paymentMethod"]:checked')?.value;
  const cardNumber = $("cardNumber").value.replace(/[\s-]/g, "");
  const expiry = $("cardExpiry").value.trim();
  const cvv = $("cardCvv").value.trim();

  if (!method) return { error: "Please select a payment method (Credit Card or Debit Card)." };
  if (!cardNumber) return { error: "Please enter the card number." };
  if (!/^\d{13,19}$/.test(cardNumber)) return { error: "Card number must contain 13 to 19 digits." };
  if (!expiry) return { error: "Please enter the expiry date." };
  const m = expiry.match(/^(0[1-9]|1[0-2])\s*\/\s*(\d{2})$/);
  if (!m) return { error: "Expiry date must be in MM / YY format." };
  const now = new Date();
  const year = 2000 + Number(m[2]);
  if (year < now.getFullYear() || (year === now.getFullYear() && Number(m[1]) < now.getMonth() + 1)) return { error: "This card has expired." };
  if (!cvv) return { error: "Please enter the CVV." };
  if (!/^\d{3,4}$/.test(cvv)) return { error: "CVV must be 3 or 4 digits." };
  return { method, cardNumber, expiryDate: `${m[1]}/${m[2]}`, cvv };
}

$("payForm").addEventListener("submit", async (e) => {
  e.preventDefault();
  $("cardErrors").classList.add("hidden");
  if (!currentBill) return showMessage("There is no current bill to pay.", "error");

  const card = validateCard();
  if (card.error) return showCardError(card.error);

  const btn = $("payBtn");
  btn.disabled = true;
  btn.textContent = "Processing...";
  try {
    // Only the Bill ID and card details are sent - never a Customer ID and never an amount.
    // NWSDB decides both: who this bill belongs to (from the JWT) and what it costs.
    const p = await api("/api/me/pay", {
      method: "POST",
      body: JSON.stringify({
        billId: currentBill.billId,
        paymentMethod: card.method,
        cardNumber: card.cardNumber,
        expiryDate: card.expiryDate,
        cvv: card.cvv,
      }),
    });

    $("rNo").textContent = p.paymentId;
    $("rBill").textContent = "#" + p.billId;
    $("rAmount").textContent = rs(p.amountPaid);
    $("rDate").textContent = p.paymentDate ? new Date(p.paymentDate).toLocaleString() : new Date().toLocaleString();
    $("rStatus").textContent = p.status || "SUCCESS";

    $("payForm").reset();                 // clear the card fields straight away
    $("receiptSection").classList.remove("hidden");
    showMessage("Payment successful and recorded in NWSDB (simulated payment).", "success");

    // Immediately refresh from NWSDB: the bill now shows Paid and Pay Bill disappears,
    // because GET /api/bill/customer/{id}/current no longer returns a bill to pay.
    await loadDashboard();
  } catch (err) {
    showMessage(err.message, "error");
  } finally {
    btn.disabled = false;
    btn.textContent = "Pay now";
  }
});

$("printReceiptBtn").addEventListener("click", () => window.print());

// ---------- boot ----------
(function init() {
  const session = getSession();
  if (session?.token) {
    showLoggedIn(session.displayName);
    loadDashboard();
  } else {
    showLoggedOut();
  }
})();
