import { Fragment, type JSX } from "react";

interface EachProps<T> {
  of: T[];
  keyExtractor: (item: T) => string;
  children?: (item: T, index: number) => JSX.Element;
}

export default function Each<T>({ of, keyExtractor, children }: EachProps<T>) {
  return (
    <>
      {of.map((item, index) => (
        <Fragment key={keyExtractor(item)}>{children ? children(item, index) : null}</Fragment>
      ))}
    </>
  );
}
