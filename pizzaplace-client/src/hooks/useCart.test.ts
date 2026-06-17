import { describe, it, expect } from 'vitest';
import { cartReducer, cartTotal } from './useCart';
import type { CartItem } from './useCart';
import type { Pizza } from '../types';

const pizza1: Pizza = { id: 1, name: 'Margherita', price: 12.5, description: 'Classic', imageUrl: null };
const pizza2: Pizza = { id: 2, name: 'Pepperoni',  price: 14.5, description: 'Spicy',   imageUrl: null };

describe('cartReducer — add', () => {
  it('creates a new item with quantity 1', () => {
    const result = cartReducer([], { type: 'add', pizza: pizza1 });
    expect(result).toHaveLength(1);
    expect(result[0]).toEqual({ pizza: pizza1, quantity: 1 });
  });

  it('increments quantity when the same pizza is added again (no duplicate line)', () => {
    const initial: CartItem[] = [{ pizza: pizza1, quantity: 1 }];
    const result = cartReducer(initial, { type: 'add', pizza: pizza1 });
    expect(result).toHaveLength(1);
    expect(result[0].quantity).toBe(2);
  });

  it('adds a second line for a different pizza', () => {
    const initial: CartItem[] = [{ pizza: pizza1, quantity: 1 }];
    const result = cartReducer(initial, { type: 'add', pizza: pizza2 });
    expect(result).toHaveLength(2);
    expect(result.find(i => i.pizza.id === pizza2.id)?.quantity).toBe(1);
  });
});

describe('cartReducer — remove', () => {
  it('removes the matching item', () => {
    const initial: CartItem[] = [
      { pizza: pizza1, quantity: 2 },
      { pizza: pizza2, quantity: 1 },
    ];
    const result = cartReducer(initial, { type: 'remove', pizzaId: pizza1.id });
    expect(result).toHaveLength(1);
    expect(result[0].pizza.id).toBe(pizza2.id);
  });

  it('empties the cart when the last item is removed', () => {
    const initial: CartItem[] = [{ pizza: pizza1, quantity: 3 }];
    const result = cartReducer(initial, { type: 'remove', pizzaId: pizza1.id });
    expect(result).toHaveLength(0);
  });

  it('leaves the cart unchanged when the id does not match', () => {
    const initial: CartItem[] = [{ pizza: pizza1, quantity: 1 }];
    const result = cartReducer(initial, { type: 'remove', pizzaId: 999 });
    expect(result).toHaveLength(1);
  });
});

describe('cartReducer — reset', () => {
  it('clears all items regardless of contents', () => {
    const initial: CartItem[] = [
      { pizza: pizza1, quantity: 2 },
      { pizza: pizza2, quantity: 1 },
    ];
    expect(cartReducer(initial, { type: 'reset' })).toHaveLength(0);
  });

  it('is a no-op on an already-empty cart', () => {
    expect(cartReducer([], { type: 'reset' })).toHaveLength(0);
  });
});

describe('cartTotal', () => {
  it('returns 0 for an empty cart', () => {
    expect(cartTotal([])).toBe(0);
  });

  it('returns price × quantity for a single item', () => {
    const items: CartItem[] = [{ pizza: pizza1, quantity: 3 }];
    expect(cartTotal(items)).toBe(12.5 * 3);
  });

  it('sums across multiple items correctly', () => {
    const items: CartItem[] = [
      { pizza: pizza1, quantity: 2 },
      { pizza: pizza2, quantity: 1 },
    ];
    expect(cartTotal(items)).toBeCloseTo(12.5 * 2 + 14.5);
  });

  it('returns 0 after all items are removed', () => {
    const items: CartItem[] = [];
    expect(cartTotal(items)).toBe(0);
  });
});
