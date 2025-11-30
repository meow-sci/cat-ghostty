import type { Atom } from "nanostores";

const promiseCache = new Map<Atom<any>, Promise<any>>();

export function useStorePromise<T>(store: Atom<T>): Promise<T> {
  if (!promiseCache.has(store)) {
    promiseCache.set(store, new Promise<T>((resolve) => {
      let unsub = store.listen((value) => {
        resolve(value);
        unsub();
      });
    }));
  }
  return promiseCache.get(store)!;
}
