﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Meta.Numerics.Matrices {

    /// <summary>
    /// Represents the QR decomposition of a square matrix.
    /// </summary>
    /// <remarks>
    /// <para>A QR decomposition represents a matrix as the product of an orthogonal matrix Q and an upper-right-triangular matrix R.</para>
    /// <para>Like a LU decomposition (<see cref="LUDecomposition"/>, a QR decomposition can be used to solve systems of equations,
    /// or compute a determinant or matrix inverse.</para>
    /// <para>To obtain the QR decomposition of a square matrix, use the <see cref="SquareMatrix.QRDecomposition"/> method of the
    /// <see cref="SquareMatrix"/> class.</para>
    /// </remarks>
    public sealed class SquareQRDecomposition {

        private readonly double[] qtStore;
        private readonly double[] rStore;
        private readonly int dimension;

        internal SquareQRDecomposition (double[] qtStore, double[] rStore, int dimension) {
            Debug.Assert(qtStore != null);
            Debug.Assert(rStore != null);
            Debug.Assert(dimension > 0);
            this.qtStore = qtStore;
            this.rStore = rStore;
            this.dimension = dimension;
        }

        /// <summary>
        /// The orthogonal matrix Q.
        /// </summary>
        /// <returns>The orthogonal matrix Q.</returns>
        /// <remarks>
        /// <para>The returned matrix is read-only. If you need to make changes to it, you can call <see cref="SquareMatrix.Copy"/> to obtain a writable copy.</para>
        /// </remarks>
        public SquareMatrix QMatrix {
            get {
                return (new SquareMatrix(qtStore, 0, dimension, 1, dimension, true));
            }
        }

        /// <summary>
        /// The upper-right triangular matrix R.
        /// </summary>
        /// <returns>The upper-right triangular matrix R.</returns>
        /// <remarks>
        /// <para>The returned matrix is read-only. If you need to make changes to it, you can call <see cref="SquareMatrix.Copy"/> to obtain a writable copy.</para>
        /// </remarks>
        public SquareMatrix RMatrix {
            get {
                return (new SquareMatrix(rStore, dimension, true));
            }
        }

        /// <summary>
        /// Solve the system of equations Ax=b, where A is the original matrix.
        /// </summary>
        /// <param name="rhs">The right-hand-side vector b.</param>
        /// <returns>The solution vector x.</returns>
        public ColumnVector Solve (IReadOnlyList<double> rhs) {

            if (rhs == null) throw new ArgumentNullException(nameof(rhs));
            if (rhs.Count != dimension) throw new DimensionMismatchException();

            double[] y = new double[rhs.Count];
            rhs.CopyTo(y, 0);

            y = MatrixAlgorithms.Multiply(qtStore, dimension, dimension, y, dimension, 1);
            SquareMatrixAlgorithms.SolveUpperRightTriangular(rStore, y, 0, dimension);

            return (new ColumnVector(y, dimension));
        }

        /// <summary>
        /// Computes the determinant of the original matrix.
        /// </summary>
        /// <returns>det A</returns>
        public double Determinant () {
            double det = 1.0;
            for (int i = 0; i < dimension; i++) {
                det *= MatrixAlgorithms.GetEntry(rStore, dimension, dimension, i, i);
            }
            return (det);
        }

        /// <summary>
        /// Computes the inverse of the original matrix.
        /// </summary>
        /// <returns>A<sup>-1</sup></returns>
        public SquareMatrix Inverse () {

            // solve R Q^T column-by-column
            double[] iStore = MatrixAlgorithms.Copy(qtStore);
            for (int c = 0; c < dimension; c++) {
                SquareMatrixAlgorithms.SolveUpperRightTriangular(rStore, iStore, dimension * c, dimension);
            }
            return (new SquareMatrix(iStore, dimension));
        }

        /// <summary>
        /// Gets the dimension of the original matrix.
        /// </summary>
        public int Dimension {
            get {
                return(dimension);
            }
        }
        
    }
}
