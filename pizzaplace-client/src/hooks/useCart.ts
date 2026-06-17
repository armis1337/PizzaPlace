import { useReducer } from 'react';
import type { Pizza } from '../types';

export interface CartItem { pizza: Pizza; quantity: number; }

type CartAction =
  | { type: 'add'; pizza: Pizza }
  | { type: 'remove'; pizzaId: number }
  | { type: 'reset' };

export function cartReducer(state: CartItem[], action: CartAction): CartItem[] {
  switch (action.type) {
    case 'add': {
      const existing = state.find(i => i.pizza.id === action.pizza.id);
      if (existing)
        return state.map(i =>
          i.pizza.id === action.pizza.id ? { ...i, quantity: i.quantity + 1 } : i
        );
      return [...state, { pizza: action.pizza, quantity: 1 }];
    }
    case 'remove':
      return state.filter(i => i.pizza.id !== action.pizzaId);
    case 'reset':
      return [];
  }
}

export function cartTotal(items: CartItem[]): number {
  return items.reduce((sum, i) => sum + i.pizza.price * i.quantity, 0);
}

export function useCart() {
  const [items, dispatch] = useReducer(cartReducer, []);
  return {
    items,
    add:    (pizza: Pizza) => dispatch({ type: 'add', pizza }),
    remove: (pizzaId: number) => dispatch({ type: 'remove', pizzaId }),
    reset:  () => dispatch({ type: 'reset' }),
    total:  cartTotal(items),
    count:  items.reduce((s, i) => s + i.quantity, 0),
  };
}
