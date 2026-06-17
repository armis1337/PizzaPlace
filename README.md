# PizzaPlace

A full-stack portfolio demo: a multi-role restaurant ordering system built with **ASP.NET Core 10**, **EF Core + SQLite**, **SignalR**, **JWT auth**, and a **React + TypeScript** frontend.

**The story it tells:** A guest orders a pizza. The order appears on the chef's screen *live* — no refresh. The chef prepares it; ingredient stock ticks down. The instant the chef marks it ready, it pops onto the delivery board *live*. Delivery claims it, marks it delivered. The supervisor watches every transition in real time and can restock inventory (with a simulated delay). Real-time, role-secured, full lifecycle.

> **Everything simulated:** payment is instant (€ total returned in the response), restocking completes after a 5-second delay.

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
| **Guest** | None | Browse menu, add to cart, place order, track live |
| **Chef** | chef / chef | See live queue, start orders (decrements stock), mark ready |
| **Delivery** | delivery / delivery | See ready orders live, claim, mark delivered |
| **Supervisor** | supervisor / supervisor | All orders, inventory + restock, summary stats |

---

## Order lifecycle

```
Received → Preparing → Ready → OutForDelivery → Delivered
 (guest)    (chef)     (chef)    (delivery)       (delivery)
```

State transitions are enforced server-side — invalid jumps return 400.

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

API listens on `http://localhost:5000`. On first run it creates `pizzaplace.db` and seeds pizzas, ingredients, and users.

### Frontend

```bash
cd pizzaplace-client
npm install
npm run dev
```

Opens at `http://localhost:5173`.

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
| GET | `/api/kitchen/orders` | Received + Preparing |
| POST | `/api/kitchen/orders/{id}/start` | → Preparing (decrements stock) |
| POST | `/api/kitchen/orders/{id}/ready` | → Ready |

### Delivery `[Authorize(Roles="Delivery")]`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/delivery/orders` | Ready + claimed |
| POST | `/api/delivery/orders/{id}/claim` | → OutForDelivery |
| POST | `/api/delivery/orders/{id}/deliver` | → Delivered |

### Supervisor `[Authorize(Roles="Supervisor")]`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/inventory` | Ingredients + low-stock flags |
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
| `OrderStatusChanged` | Supervisor group | Every transition |
| `InventoryChanged` | Supervisor group | Stock decrements or restock completes |

---

## Seeded credentials

| Role | Username | Password |
|------|----------|----------|
| Chef | `chef` | `chef` |
| Delivery | `delivery` | `delivery` |
| Supervisor | `supervisor` | `supervisor` |

Guest access requires no login — just open the app and order.
