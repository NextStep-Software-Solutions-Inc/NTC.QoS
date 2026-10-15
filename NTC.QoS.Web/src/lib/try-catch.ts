// Utility function to wrap async calls in try/catch.
// It returns a tuple: [T, null] on success, or [null, Error] on failure.
// This keeps the existing array-style API but adds a type-guard to narrow the tuple.
export type TryCatchTupleSuccess<T> = [T, null];
export type TryCatchTupleFailure = [null, Error];
export type TryCatchTupleResult<T> = TryCatchTupleSuccess<T> | TryCatchTupleFailure;

export async function tryCatch<T>(asyncFn: () => Promise<T>): Promise<TryCatchTupleResult<T>> {
  try {
    const result = await asyncFn();
    return [result, null];
  } catch (error) {
    return [null, error as Error];
  }
}
