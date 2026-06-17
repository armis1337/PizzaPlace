

# PizzaPlace

A full-stack portfolio demo: a multi-role restaurant ordering system built with **ASP.NET Core 10**, **EF Core + SQLite**, **SignalR**, **JWT auth**, and a **React + TypeScript** frontend.

**The story it tells:** A guest orders a pizza. The order appears on the chef's screen *live* — no refresh. The chef prepares it; ingredient stock ticks down. The instant the chef marks it ready, it pops onto the delivery board *live*. Delivery claims it, marks it delivered. The supervisor watches every transition in real time and can restock inventory (with a simulated delay). Real-time, role-secured, full lifecycle.

## Demo

[![PizzaPlace demo video](https://img.youtube.com/vi/MakP8rLzrT4/maxresdefault.jpg)](https://youtu.be/MakP8rLzrT4)

*Full order lifecycle across all four roles — guest orders, kitchen and delivery update live, supervisor handles a stock shortage.*

> **Everything simulated:** payment is instant (€ total returned in the response), restocking completes after a ~5-second delay.

---

## Tech stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 10 Web API |
| ORM / DB | EF Core 10 + SQLite |
| Real-time | SignalR |
| Auth | JWT bearer tokens + role claims |
| Frontend | React 18 + TypeScript + Vite |
| HTTP client | Axios |
| Real-time client | @microsoft/signalr |

---

## Roles

| Role | Login | Can do |
|------|-------|--------|
| **Guest** | None | Browse menu, add to cart, place order, track live, dismiss a finished order |
| **Chef** | chef / chef | See live queue, start orders (decrements stock), mark ready, cancel a received order |
| **Delivery** | delivery / delivery | See ready orders live, claim, mark delivered |
| **Supervisor** | supervisor / supervisor | All orders, inventory + restock, shortage warnings, summary stats |

---

## Order lifecycle

```
Received → Preparing → Ready → OutForDelivery → Delivered   (terminal)
 (guest)    (chef)     (chef)    (delivery)       (delivery)
   │
   └─→ Cancelled   (terminal — chef cancels a Received order, with a reason)
```

State transitions are enforced server-side — invalid jumps return 400. A `Cancelled` order can only be reached from `Received`; both `Delivered` and `Cancelled` are terminal.

---

## Notable features

- **Cancellation with reason** — the chef can cancel an order while it's still `Received`, attaching a free-text reason. Cancelled orders remain in history (and in the supervisor's all-orders board) but are excluded from revenue, since they were never fulfilled.
- **Shortage warnings** — the supervisor's inventory view flags each ingredient whose total demand from pending (`Received`) orders exceeds current stock, showing the deficit plus a hover tooltip breakdown ("needed X across N orders · have Y · short Z").
- **Can't-start guard** — the chef's "Start Preparing" button disables live for any order that can't be fulfilled with current stock, and re-enables automatically once a restock makes it possible (recomputed against live stock, no page refresh).
- **Dismissable tracker** — once a guest's order reaches a terminal state (`Delivered` or `Cancelled`), they can dismiss the tracking panel to return to a clean menu. The order still exists server-side.

---

## Running locally

### Prerequisites
- .NET 10 SDK
- Node.js 22+

### Backend

```bash
cd PizzaPlace.Api
dotnet run
```

API listens on `http://localhost:5221`. On first run it creates `pizzaplace.db` and seeds pizzas, ingredients, and users.

### Frontend

```bash
cd pizzaplace-client
npm install
npm run dev
```

Opens at `http://localhost:5173`. The dev server reads the API base URL from `.env` (`VITE_API_BASE=http://localhost:5221`), which must match the API port above — a fresh clone is already configured to agree.

---

## Testing

### Backend — 38 tests (xUnit)

Integration tests run the real app through `WebApplicationFactory<Program>`, each against its **own isolated SQLite database** (a fresh temp file per test class), so they exercise the full HTTP → controller → service → EF Core stack without mocks. Coverage includes:

- **Ordering** — placement, totals, validation (empty name, no items, unknown pizza).
- **Auth / authorization** — login, JWT issuance, role-protected endpoints.
- **Stock** — decrement on start, the insufficient-stock guard (400 + no partial deduction), and the multi-order over-draw case.
- **Full lifecycle** — Received → Preparing → Ready → OutForDelivery → Delivered end to end.
- **Cancellation** — Received-only guard, reason persistence, exclusion from revenue, still present in history.
- **Shortage calculation** — aggregate demand vs stock, per-order `canStart` (including the dynamic sequential case), and the pure `ShortageCalculator` unit tests.

```bash
cd PizzaPlace.Api.Tests && dotnet test
```

### Frontend — 40 tests (Vitest + React Testing Library)

Component and unit tests covering cart logic (the `cartReducer` and totals), API error extraction, and component interactions for the guest, chef, delivery, and supervisor views — placing/clearing the cart, the cancel modal, disabled "Start" when stock is short, the shortage badge + tooltip, and dismissing a finished order's tracker.

```bash
cd pizzaplace-client && npm run test
```

---

## API surface

### Public
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/menu` | All pizzas |
| POST | `/api/orders` | Place an order |
| GET | `/api/orders/{id}` | Track an order |

### Auth
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Returns JWT + role |

### Chef `[Authorize(Roles="Chef")]`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/kitchen/orders` | Received + Preparing (each with a live `canStart` flag) |
| POST | `/api/kitchen/orders/{id}/start` | → Preparing (decrements stock) |
| POST | `/api/kitchen/orders/{id}/ready` | → Ready |
| POST | `/api/kitchen/orders/{id}/cancel` | → Cancelled (Received only; optional reason body) |

### Delivery `[Authorize(Roles="Delivery")]`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/delivery/orders` | Ready + claimed |
| POST | `/api/delivery/orders/{id}/claim` | → OutForDelivery |
| POST | `/api/delivery/orders/{id}/deliver` | → Delivered |

### Supervisor `[Authorize(Roles="Supervisor")]`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/inventory` | Ingredients + low-stock & shortage flags (demand, deficit) |
| POST | `/api/inventory/{id}/restock` | Trigger simulated restock |
| GET | `/api/orders` | All orders (optional `?status=` filter) |
| GET | `/api/stats/summary` | Totals: orders today, revenue, top pizza |

---

## SignalR events

Hub: `/hubs/orders`

| Event | Sent to | When |
|-------|---------|------|
| `OrderReceived` | Chef group | Guest places order |
| `OrderReady` | Delivery group | Chef marks order Ready |
| `OrderStatusChanged` | Supervisor + Guest groups | Every transition (the guest tracks their own order live) |
| `InventoryChanged` | Supervisor + Chef groups | Stock decrements, or a restock starts/completes (drives shortage + can't-start updates) |

Clients join role groups on connect and **re-join automatically after a reconnect**, so live updates survive an API restart or dropped connection.

---

## Architecture notes

A few deliberate design choices behind the code:

- **Thin controllers, service layer.** Controllers just translate HTTP to a service call and back; all business logic lives in services that return typed DTOs. A `Result<T>` type carries expected failures (not found, bad request, insufficient stock) so services never throw for control flow or leak HTTP concerns — the base controller maps `Result<T>` to the right status code in one place.
- **Atomic stock handling.** Starting an order runs the stock check *and* decrement inside a single transaction, with demand aggregated per ingredient first. Two pizzas in one order that share an ingredient are summed before the check, so stock can never go negative — and partial deductions never happen on a rejected order.
- **Derived, not stored, shortage state.** Per-ingredient shortage (demand vs stock) and per-order "can start now" are computed live from current stock and pending orders on each read — never persisted. There's no status field to keep in sync, so they're always correct and update automatically as orders and stock change.
- **Two-sided real-time.** REST handles request/response and initial load; SignalR handles server-pushed updates. The client fetches on mount and then layers live events on top of that state, rather than polling. Hub connections auto-reconnect and re-join their role group.
- **Centralized roles & enums, typed end to end.** Roles, order statuses, and hub group names are single-source enums on the backend; the frontend mirrors the API shapes in TypeScript, so a DTO change surfaces as a compile error rather than a runtime surprise.

---

## Seeded credentials

| Role | Username | Password |
|------|----------|----------|
| Chef | `chef` | `chef` |
| Delivery | `delivery` | `delivery` |
| Supervisor | `supervisor` | `supervisor` |

Guest access requires no login — just open the app and order.

---

## Roadmap / possible future additions

Deliberately out of scope for this build, but natural next steps:

- **Staff-to-staff real-time chat** — e.g. chef ↔ supervisor messaging over the existing SignalR hub, for "out of dough, hold orders" coordination.
- **Employee management panel** — let a supervisor add/remove staff and assign roles at runtime, instead of the current seeded users.
- **Guest accounts & order history** — persistent accounts so guests can see past orders; tracking is currently per-session only.
- **Real payment integration** — replace the simulated instant charge with a real provider (e.g. Stripe) and proper payment states.
- **Order ratings & feedback** — let guests rate a delivered order, feeding into the supervisor's stats.
