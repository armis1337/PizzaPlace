export type OrderStatus =
  | 'Received'
  | 'Preparing'
  | 'Ready'
  | 'OutForDelivery'
  | 'Delivered'
  | 'Cancelled';

export interface Pizza {
  id: number;
  name: string;
  description: string;
  price: number;
  imageUrl: string | null;
}

export interface OrderItem {
  pizzaId: number;
  pizzaName: string;
  quantity: number;
}

export interface Order {
  id: number;
  customerName: string;
  status: OrderStatus;
  createdAt: string;
  updatedAt: string;
  totalPrice: number;
  claimedByDeliveryUser: string | null;
  cancellationReason: string | null;
  items: OrderItem[];
  // Kitchen-only: whether this order fits in current stock right now.
  canStart?: boolean;
  blockingIngredients?: string[];
}

export interface Ingredient {
  id: number;
  name: string;
  stockQuantity: number;
  unit: string;
  lowStockThreshold: number;
  isRestocking: boolean;
  isLow: boolean;
  demandFromOrders: number;
  hasShortage: boolean;
  deficit: number;
  ordersWithDemand: number;
}

export interface Stats {
  ordersToday: number;
  revenueToday: number;
  revenueTotal: number;
  totalOrders: number;
  delivered: number;
  mostPopularPizza: { pizza: string; count: number } | null;
}

export interface AuthState {
  token: string;
  role: 'Chef' | 'Delivery' | 'Supervisor';
  username: string;
}
