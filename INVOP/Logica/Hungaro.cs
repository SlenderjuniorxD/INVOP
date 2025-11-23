using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INVOP.Logica
{
    public class Hungaro
    {
        public class PasoHungaro
        {
            public string Titulo { get; set; }      // Ej: "Paso 1: Resta de Filas"
            public double[,] Matriz { get; set; }   // La foto de los números
            public string Descripcion { get; set; } // Explicación de qué se hizo

            public List<int> FilasTachadas { get; set; } = new List<int>();
            public List<int> ColsTachadas { get; set; } = new List<int>();
        }
        public class ResultadoHungaro
        {
            public List<PasoHungaro> Historial { get; set; } = new List<PasoHungaro>();
            public double CostoTotal { get; set; }
            public List<string> Asignaciones { get; set; } = new List<string>(); // Ej: "Trabajador 1 -> Tarea 3"
        }
        public class HungarianSolver
        {
            public ResultadoHungaro Resolver(double[,] matrizEntrada)
            {
                var resultado = new ResultadoHungaro();

                // 1. BALANCEO Y COPIA
                double[,] matriz = BalancearMatriz(matrizEntrada, resultado);
                int n = matriz.GetLength(0);
                double[,] costosOriginales = CopiarMatriz(matriz);

                // 2. REDUCCIÓN DE FILAS
                for (int i = 0; i < n; i++)
                {
                    double min = double.MaxValue;
                    for (int j = 0; j < n; j++) if (matriz[i, j] < min) min = matriz[i, j];
                    for (int j = 0; j < n; j++) matriz[i, j] -= min;
                }
                resultado.Historial.Add(new PasoHungaro { Titulo = "Paso 1: Reducción de Filas", Matriz = CopiarMatriz(matriz), Descripcion = "Se restó el menor valor de cada fila." });

                // 3. REDUCCIÓN DE COLUMNAS
                for (int j = 0; j < n; j++)
                {
                    double min = double.MaxValue;
                    for (int i = 0; i < n; i++) if (matriz[i, j] < min) min = matriz[i, j];
                    for (int i = 0; i < n; i++) matriz[i, j] -= min;
                }
                resultado.Historial.Add(new PasoHungaro { Titulo = "Paso 2: Reducción de Columnas", Matriz = CopiarMatriz(matriz), Descripcion = "Se restó el menor valor de cada columna." });

                int iteracion = 1;
                while (true)
                {
                    int[] matchFila;
                    ObtenerAsignacion(matriz, out matchFila);

                    bool[] filasCubiertas;
                    bool[] colsCubiertas;
                    ObtenerLineasMinimas(matriz, matchFila, out filasCubiertas, out colsCubiertas);

                    int numLineas = 0;
                    var indicesFilas = new List<int>();
                    var indicesCols = new List<int>();

                    for (int i = 0; i < n; i++) if (filasCubiertas[i]) { numLineas++; indicesFilas.Add(i); }
                    for (int j = 0; j < n; j++) if (colsCubiertas[j]) { numLineas++; indicesCols.Add(j); }

                    // --- GUARDAR PASO VISUAL DE LÍNEAS ---
                    // Aquí guardamos la matriz tal cual está, pero indicando qué pintar
                    resultado.Historial.Add(new PasoHungaro
                    {
                        Titulo = $"Iteración {iteracion}: Trazado de Líneas",
                        Matriz = CopiarMatriz(matriz),
                        FilasTachadas = new List<int>(indicesFilas),
                        ColsTachadas = new List<int>(indicesCols)
                    });

                    // CRITERIO DE PARADA: Si líneas == n, terminamos
                    if (numLineas == n)
                    {
                        // Calcular asignación final visualizable
                        GenerarResultadoFinal(matriz, costosOriginales, resultado);
                        break;
                    }

                    // B. AJUSTE (Valor K)
                    double k = double.MaxValue;
                    // Buscar menor no cubierto
                    for (int i = 0; i < n; i++)
                    {
                        if (!filasCubiertas[i])
                        {
                            for (int j = 0; j < n; j++)
                            {
                                if (!colsCubiertas[j])
                                {
                                    if (matriz[i, j] < k) k = matriz[i, j];
                                }
                            }
                        }
                    }

                    // Aplicar K
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < n; j++)
                        {
                            if (!filasCubiertas[i] && !colsCubiertas[j]) matriz[i, j] -= k; // No cubierto
                            else if (filasCubiertas[i] && colsCubiertas[j]) matriz[i, j] += k; // Intersección
                        }
                    }

                    resultado.Historial.Add(new PasoHungaro
                    {
                        Titulo = $"Iteración {iteracion}",
                        Matriz = CopiarMatriz(matriz),
                        //Descripcion = $"Como {numLineas} líneas < {n}, ajustamos. Restamos K={k} a no cubiertos y sumamos a intersecciones."
                    });

                    iteracion++;
                    if (iteracion > 20) break; // Seguridad
                }

                return resultado;
            }

            // --- MÉTODOS DE SOPORTE (KÖNIG Y OTROS) ---
            // (Estos se mantienen igual que en la versión anterior que sí funcionaba matemáticamente)

            private void ObtenerLineasMinimas(double[,] matriz, int[] matchFila, out bool[] filasCubiertas, out bool[] colsCubiertas)
            {
                int n = matriz.GetLength(0);
                bool[] filaMarcada = new bool[n];
                bool[] colMarcada = new bool[n];

                var cola = new Queue<int>();
                for (int i = 0; i < n; i++)
                {
                    if (matchFila[i] == -1) { filaMarcada[i] = true; cola.Enqueue(i); }
                }

                while (cola.Count > 0)
                {
                    int fila = cola.Dequeue();
                    for (int j = 0; j < n; j++)
                    {
                        if (Math.Abs(matriz[fila, j]) < 0.00001 && !colMarcada[j])
                        {
                            colMarcada[j] = true;
                            for (int k = 0; k < n; k++)
                            {
                                if (matchFila[k] == j && !filaMarcada[k])
                                {
                                    filaMarcada[k] = true; cola.Enqueue(k); break;
                                }
                            }
                        }
                    }
                }
                filasCubiertas = new bool[n]; colsCubiertas = new bool[n];
                for (int i = 0; i < n; i++) filasCubiertas[i] = !filaMarcada[i];
                for (int j = 0; j < n; j++) colsCubiertas[j] = colMarcada[j];
            }

            private int ObtenerAsignacion(double[,] matriz, out int[] matchFila)
            {
                int n = matriz.GetLength(0);
                matchFila = new int[n];
                for (int i = 0; i < n; i++) matchFila[i] = -1;
                int[] matchCol = new int[n];
                for (int j = 0; j < n; j++) matchCol[j] = -1;
                int asignados = 0;
                for (int u = 0; u < n; u++)
                {
                    bool[] visitado = new bool[n];
                    if (Dfs(u, matriz, visitado, matchCol)) asignados++;
                }
                for (int j = 0; j < n; j++) if (matchCol[j] != -1) matchFila[matchCol[j]] = j;
                return asignados;
            }

            private bool Dfs(int u, double[,] matriz, bool[] visitado, int[] matchCol)
            {
                int n = matriz.GetLength(0);
                for (int v = 0; v < n; v++)
                {
                    if (Math.Abs(matriz[u, v]) < 0.00001 && !visitado[v])
                    {
                        visitado[v] = true;
                        if (matchCol[v] < 0 || Dfs(matchCol[v], matriz, visitado, matchCol))
                        {
                            matchCol[v] = u; return true;
                        }
                    }
                }
                return false;
            }

            private double[,] BalancearMatriz(double[,] original, ResultadoHungaro res)
            {
                int f = original.GetLength(0); int c = original.GetLength(1);
                if (f == c) return CopiarMatriz(original);
                int max = Math.Max(f, c);
                double[,] cuadrada = new double[max, max];
                for (int i = 0; i < f; i++) for (int j = 0; j < c; j++) cuadrada[i, j] = original[i, j];
                res.Historial.Add(new PasoHungaro { Titulo = "Balanceo", Matriz = CopiarMatriz(cuadrada), Descripcion = "Se agregaron filas/columnas ficticias (ceros)." });
                return cuadrada;
            }

            private void GenerarResultadoFinal(double[,] matrizFinal, double[,] costos, ResultadoHungaro res)
            {
                // Usamos el algoritmo de asignación una última vez para sacar la lista limpia
                int[] matchFila;
                ObtenerAsignacion(matrizFinal, out matchFila);

                double total = 0;
                for (int i = 0; i < matrizFinal.GetLength(0); i++)
                {
                    int tarea = matchFila[i];
                    if (tarea != -1)
                    {
                        double costo = costos[i, tarea];
                        total += costo;
                        res.Asignaciones.Add($"Fila {i + 1} -> Columna {tarea + 1} (Costo: {costo})");
                    }
                }
                res.CostoTotal = total;
            }

            private double[,] CopiarMatriz(double[,] original)
            {
                int f = original.GetLength(0); int c = original.GetLength(1);
                double[,] copia = new double[f, c];
                Array.Copy(original, copia, original.Length);
                return copia;
            }
        }
    }
}
