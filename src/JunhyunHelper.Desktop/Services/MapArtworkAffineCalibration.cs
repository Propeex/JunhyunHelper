namespace JunhyunHelper.Desktop.Services;

public readonly record struct MapArtworkCalibrationPair(
    double U,
    double V,
    double SurfaceX,
    double SurfaceY);

public readonly record struct MapArtworkAffineTransform(
    double A,
    double B,
    double C,
    double D,
    double E,
    double F)
{
    public (double X, double Y) Apply(double u, double v) =>
        (A * u + B * v + C, D * u + E * v + F);
}

public static class MapArtworkAffineCalibration
{
    public static bool TryFit(
        IReadOnlyList<MapArtworkCalibrationPair> pairs,
        out MapArtworkAffineTransform transform,
        out double residual,
        out double maxError)
    {
        transform = default;
        residual = maxError = double.PositiveInfinity;
        if (pairs.Count < 3)
            return false;

        double sUU = 0, sUV = 0, sU = 0, sVV = 0, sV = 0;
        double xU = 0, xV = 0, x1 = 0, yU = 0, yV = 0, y1 = 0;
        foreach (var pair in pairs)
        {
            sUU += pair.U * pair.U;
            sUV += pair.U * pair.V;
            sU += pair.U;
            sVV += pair.V * pair.V;
            sV += pair.V;
            xU += pair.U * pair.SurfaceX;
            xV += pair.V * pair.SurfaceX;
            x1 += pair.SurfaceX;
            yU += pair.U * pair.SurfaceY;
            yV += pair.V * pair.SurfaceY;
            y1 += pair.SurfaceY;
        }

        var matrix = new[,]
        {
            { sUU, sUV, sU },
            { sUV, sVV, sV },
            { sU, sV, (double)pairs.Count },
        };
        if (!TrySolve3x3(matrix, [xU, xV, x1], out var x) ||
            !TrySolve3x3(matrix, [yU, yV, y1], out var y))
            return false;

        var solved = new MapArtworkAffineTransform(x[0], x[1], x[2], y[0], y[1], y[2]);
        var errors = new double[pairs.Count];
        double squared = 0;
        double maximum = 0;
        for (var index = 0; index < pairs.Count; index++)
        {
            var error = Error(solved, pairs[index]);
            errors[index] = error;
            squared += error * error;
            maximum = Math.Max(maximum, error);
        }

        transform = solved;
        residual = Math.Sqrt(squared / pairs.Count);
        maxError = maximum;
        return double.IsFinite(residual) && double.IsFinite(maxError);
    }

    public static bool LooksSane(MapArtworkAffineTransform transform)
    {
        var determinant = transform.A * transform.E - transform.B * transform.D;
        if (!double.IsFinite(determinant) || Math.Abs(determinant) < 0.05 || Math.Abs(determinant) > 30)
            return false;

        var corners = new[]
        {
            transform.Apply(0, 0),
            transform.Apply(1, 0),
            transform.Apply(0, 1),
            transform.Apply(1, 1),
        };
        return corners.All(point =>
            point.X >= -2.0 && point.X <= 3.0 &&
            point.Y >= -2.0 && point.Y <= 3.0);
    }

    private static double Error(
        MapArtworkAffineTransform transform,
        MapArtworkCalibrationPair pair)
    {
        var mapped = transform.Apply(pair.U, pair.V);
        var dx = mapped.X - pair.SurfaceX;
        var dy = mapped.Y - pair.SurfaceY;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool TrySolve3x3(double[,] source, double[] right, out double[] solution)
    {
        solution = new double[3];
        var augmented = new double[3, 4];
        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
                augmented[row, column] = source[row, column];
            augmented[row, 3] = right[row];
        }

        for (var pivot = 0; pivot < 3; pivot++)
        {
            var best = pivot;
            for (var row = pivot + 1; row < 3; row++)
            {
                if (Math.Abs(augmented[row, pivot]) > Math.Abs(augmented[best, pivot]))
                    best = row;
            }
            if (Math.Abs(augmented[best, pivot]) < 1e-9)
                return false;

            if (best != pivot)
            {
                for (var column = pivot; column < 4; column++)
                    (augmented[pivot, column], augmented[best, column]) =
                        (augmented[best, column], augmented[pivot, column]);
            }

            var divisor = augmented[pivot, pivot];
            for (var column = pivot; column < 4; column++)
                augmented[pivot, column] /= divisor;

            for (var row = 0; row < 3; row++)
            {
                if (row == pivot)
                    continue;
                var factor = augmented[row, pivot];
                for (var column = pivot; column < 4; column++)
                    augmented[row, column] -= factor * augmented[pivot, column];
            }
        }

        for (var row = 0; row < 3; row++)
            solution[row] = augmented[row, 3];
        return solution.All(double.IsFinite);
    }
}
