import { describe, expect, it } from "vitest";
import { render } from "@testing-library/react";
import { highlight } from "./highlight";

describe("highlight", () => {
  it("wraps the case-insensitive match in mark", () => {
    const { container } = render(<div>{highlight("Alpha Beta", "alpha")}</div>);
    expect(container.querySelector("mark")?.textContent).toBe("Alpha");
  });

  it("returns plain text for an empty query", () => {
    const { container } = render(<div>{highlight("Alpha", "  ")}</div>);
    expect(container.querySelector("mark")).toBeNull();
    expect(container.textContent).toBe("Alpha");
  });

  it("returns plain text when there is no match", () => {
    const { container } = render(<div>{highlight("Alpha", "zzz")}</div>);
    expect(container.querySelector("mark")).toBeNull();
  });
});
