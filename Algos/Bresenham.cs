namespace RvB.Graphs;

/// <summary>
/// Implements Bresenham algorithms for determining points on a line or circle in a discrete space.
/// </summary>
public static class Bresenham {
    /// <summary>
    /// Enumerates discrete points between (and including) <paramref name="start"/> and <paramref name="end"/> using <see href="https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm">Bresenham algorithm</see>.
    /// </summary>
    /// <param name="start">Tuple specifying start point in X Y coordinates</param>
    /// <param name="end">Tuple specifying end point in X Y coordinates</param>
    /// <returns></returns>
    public static IEnumerable<(int X, int Y)> EnumerateLinePoints((int X, int Y) start, (int X, int Y) end) {
        var dx = Math.Abs(end.X - start.X);
        int sx = (start.X < end.X) ? 1 : -1;
        var dy = -Math.Abs(end.Y - start.Y);
        int sy = (start.Y < end.Y) ? 1 : -1;

        var e = dx + dy;
        while (true) {
            yield return start;
            if (start == end)
                break;
            var e2 = 2 * e;
            if (e2 >= dy) {
                if (start.X == end.X)
                    break;
                e += dy;
                start.X += sx;
            }
            if (e2 <= dx) {
                if (start.Y == end.Y)
                    break;
                e += dx;
                start.Y += sy;
            }
        }
    }

    /// <summary>
    /// Enumerates discrete points on a circle specified by its <paramref name="center"/> and <paramref name="radius"/>.
    /// </summary>
    /// <param name="center">Center of the circle in X Y coordinates</param>
    /// <param name="radius">Radius of the circle</param>
    /// <returns></returns>
    public static IEnumerable<(int X, int Y)> EnumerateCirclePoints((int X, int Y) center, int radius) {
        int d = (5 - radius * 4) / 4;
        int x = 0;
        int y = radius;

        do {
            if (x == 0 || y == 0 || x == y) {
                yield return (center.X + x, center.Y + y);  //  Q0
                yield return (center.X + y, center.Y - x);  //  Q2
                yield return (center.X - x, center.Y - y);  //  Q4
                yield return (center.X - y, center.Y + x);  //  Q6
            } else {
                yield return (center.X + x, center.Y + y);  //  Q0
                yield return (center.X + y, center.Y - x);  //  Q2
                yield return (center.X - x, center.Y - y);  //  Q4
                yield return (center.X - y, center.Y + x);  //  Q6
                yield return (center.X + y, center.Y + x);  // -Q1
                yield return (center.X + x, center.Y - y);  // -Q3
                yield return (center.X - y, center.Y - x);  // -Q5
                yield return (center.X - x, center.Y + y);  // -Q7
            }
            if (d < 0) {
                d += 2 * x + 1;
            } else {
                d += 2 * (x - y) + 1;
                y -= 1;
            }
            x += 1;
        } while (x <= y);
    }

    /// <summary>
    /// Enumerates discrete points on an ellipse specified by its bounding box (<paramref name="x0"/>, <paramref name="y0"/>) - (<paramref name="x1"/>, <paramref name="y1"/>).
    /// </summary>
    /// <param name="x0"></param>
    /// <param name="y0"></param>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <returns></returns>
    public static IEnumerable<(int X, int Y)> EnumerateEllipsePoints(int x0, int y0, int x1, int y1) {
        if (x0 > x1) {
            (x0, x1) = (x1, x0);
        }
        if (y0 > y1) {
            (y0, y1) = (y1, y0);
        }
        // values of diameters
        int a = x1 - x0;
        int b = y1 - y0;
        int b1 = b & 1;

        // error increments
        long dx = 4 * (1 - a) * b * b;
        long dy = 4 * (b1 + 1) * a * a;

        // error of 1st step
        long err = dx + dy + b1 * a * a, e2;

        // starting point
        y0 += (b + 1) / 2;
        y1 = y0 - b1;

        a *= 8 * a;
        b1 = 8 * b * b;
        do {
            yield return (x1, y0); // 1st Quadrant
            yield return (x0, y0); // 2nd Quadrant
            yield return (x0, y1); // 3rd Quadrant
            yield return (x1, y1); // 4th Quadrant
            e2 = 2 * err;
            if (e2 <= dy) { // y step
                y0 += 1;
                y1 -= 1;
                err += dy += a;
            }
            if (e2 >= dx || 2 * err > dy) { // x step
                x0 += 1;
                x1 -= 1;
                err += dx += b1;
            }
        } while (x0 <= x1);
        while (y0 - y1 < b) { // too early stop of flat ellipses a=1
            // -> finish tip of ellipse
            yield return (x0 - 1, y0);
            yield return (x1 + 1, y0);
            yield return (x0 - 1, y1);
            yield return (x1 + 1, y1);
            y0 += 1;
            y1 -= 1;
        }
    }
}
