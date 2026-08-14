using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GestorTareas
{

    //  ENUMERACIONES Y EXCEPCIONES


    public enum EstadoTarea
    {
        Pendiente,
        Completada
    }

    public class TareaNoEncontradaException : Exception
    {
        public TareaNoEncontradaException(string mensaje) : base(mensaje) { }
    }

    //Modelado de onjetos de tipo Tarea

    public class Tarea
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
        public EstadoTarea Estado { get; set; }

        public Tarea(int id, string titulo, string descripcion, DateTime fecha, EstadoTarea estado)
        {
            Id = id;
            Titulo = titulo;
            Descripcion = descripcion;
            Fecha = fecha;
            Estado = estado;
        }

        public string ALinea()
        {
            return string.Join("|", Id.ToString(), Titulo, Descripcion, Fecha.ToString("yyyy-MM-dd"), Estado.ToString());
        }

        public static Tarea DesdeLinea(string linea)
        {
            string[] partes = linea.Split('|');

            if (partes.Length != 5)
            {
                throw new FormatException("La linea del archivo no tiene el formato esperado (se esperaban 5 campos).");
            }

            int id = int.Parse(partes[0]);
            string titulo = partes[1];
            string descripcion = partes[2];
            DateTime fecha = DateTime.Parse(partes[3]);
            EstadoTarea estado = (EstadoTarea)Enum.Parse(typeof(EstadoTarea), partes[4]);

            return new Tarea(id, titulo, descripcion, fecha, estado);
        }

        public override string ToString()
        {
            string indicador = Estado == EstadoTarea.Completada ? "[X]" : "[ ]";
            return $"{indicador} ID: {Id} | {Titulo} - {Descripcion} (Vence: {Fecha:dd/MM/yyyy})";
        }
    }

    
    class Program
    {
        private static readonly string rutaArchivo = "tareas.txt";
        private static List<Tarea> listaTareas = new List<Tarea>();

        static void Main(string[] args)
        {
            CargarTareas();
            bool continuar = true;

            while (continuar)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=======================================");
                Console.WriteLine("          GESTOR DE TAREAS             ");
                Console.WriteLine("=======================================");
                Console.ResetColor();
                Console.WriteLine("1.  Agregar tarea");
                Console.WriteLine("2.  Listar tareas");
                Console.WriteLine("3.  Completar tarea");
                Console.WriteLine("4.  Eliminar tarea");
                Console.WriteLine("5.  Salir");
                Console.Write("\nSeleccione una opción: ");

                string entrada = Console.ReadLine();

               
                switch (entrada)
                {
                    case "1":
                        EnvolverAccion(AgregarTarea);
                        break;
                    case "2":
                        EnvolverAccion(ListarTareas);
                        break;
                    case "3":
                        EnvolverAccion(CompletarTarea);
                        break;
                    case "4":
                        EnvolverAccion(EliminarTarea);
                        break;
                    case "5":
                        Console.WriteLine("\nGuardando cambios finales y saliendo del sistema... ¡Buen día!");
                        continuar = false;
                        break;
                    default:
                        MostrarMensaje("Opción no válida. Elija un número de opción del 1 al 5.", ConsoleColor.DarkRed);
                        break;
                }
            }
        }

        private static void EnvolverAccion(Action accion)
        {
            try
            {
                accion();
            }
            catch (TareaNoEncontradaException ex)
            {
                MostrarMensaje("[Error de Búsqueda] " + ex.Message, ConsoleColor.Yellow);
            }
            catch (ArgumentException ex)
            {
                MostrarMensaje("[Entrada Inválida] " + ex.Message, ConsoleColor.DarkYellow);
            }
            catch (IOException ex)
            {
                MostrarMensaje("[Error de Persistencia] No se pudo guardar/leer en el disco: " + ex.Message, ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                MostrarMensaje("[Error Crítico] Ocurrió un fallo inesperado: " + ex.Message, ConsoleColor.Red);
            }
        }

        private static void MostrarMensaje(string mensaje, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("\n" + mensaje);
            Console.ResetColor();
            Console.WriteLine("Presione cualquier tecla para continuar...");
            Console.ReadKey();
        }

        
        // OPERACIONES DEL CRUD
        // 

        static void AgregarTarea()
        {
            Console.Clear();
            Console.WriteLine("--- AGREGAR NUEVA TAREA ---");

            Console.Write("Título (Obligatorio): ");
            string titulo = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(titulo))
                throw new ArgumentException("El título de la tarea no puede estar vacío.");

            Console.Write("Descripción: ");
            string desc = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(desc)) desc = "Sin descripción";

            Console.Write("Fecha de vencimiento (dd/mm/aaaa): ");
            DateTime fecha;
            if (!DateTime.TryParse(Console.ReadLine(), out fecha))
            {
                throw new ArgumentException("El formato de fecha ingresado es incorrecto.");
            }

            int nuevoId = listaTareas.Count > 0 ? listaTareas.Max(t => t.Id) + 1 : 1;

            Tarea nuevaTarea = new Tarea(nuevoId, titulo.Trim(), desc.Trim(), fecha, EstadoTarea.Pendiente);
            listaTareas.Add(nuevaTarea);

            GuardarTareas();
            MostrarMensaje("¡Tarea agregada exitosamente!", ConsoleColor.Green);
        }

        static void ListarTareas()
        {
            Console.Clear();
            Console.WriteLine("--- LISTA DE TAREAS REGISTRADAS ---");

            if (listaTareas.Count == 0)
            {
                Console.WriteLine("No hay tareas guardadas.");
                Console.WriteLine("\nPresione cualquier tecla para volver...");
                Console.ReadKey();
                return;
            }

            foreach (var tarea in listaTareas)
            {
                Console.WriteLine(tarea.ToString());
            }
            Console.WriteLine("\nPresione cualquier tecla para volver...");
            Console.ReadKey();
        }

        static void CompletarTarea()
        {
            Console.Clear();
            Console.WriteLine("--- COMPLETAR TAREA ---");
            if (listaTareas.Count == 0) { Console.WriteLine("No hay tareas registradas."); Console.ReadKey(); return; }

            foreach (var t in listaTareas) Console.WriteLine(t.ToString());

            Console.Write("\nIngrese el ID de la tarea a marcar como completada: ");
            int id = ValidarYObtenerId(Console.ReadLine());

            var tarea = listaTareas.First(t => t.Id == id);
            tarea.Estado = EstadoTarea.Completada;

            GuardarTareas();
            MostrarMensaje("¡La tarea con ID " + id + " se marcó como completada!", ConsoleColor.Green);
        }

        static void EliminarTarea()
        {
            Console.Clear();
            Console.WriteLine("--- ELIMINAR TAREA ---");
            if (listaTareas.Count == 0) { Console.WriteLine("No hay tareas registradas."); Console.ReadKey(); return; }

            foreach (var t in listaTareas) Console.WriteLine(t.ToString());

            Console.Write("\nIngrese el ID de la tarea que desea eliminar: ");
            int id = ValidarYObtenerId(Console.ReadLine());

            listaTareas.RemoveAll(t => t.Id == id);

            GuardarTareas();
            MostrarMensaje("¡La tarea con ID " + id + " fue eliminada del sistema!", ConsoleColor.Green);
        }

        private static int ValidarYObtenerId(string entrada)
        {
            int id;
            if (!int.TryParse(entrada, out id) || !listaTareas.Any(t => t.Id == id))
            {
                throw new TareaNoEncontradaException("El ID especificado no coincide con ninguna tarea activa en la lista.");
            }
            return id;
        }

        //  PERSISTENCIA DE DATOS 
       

        static void GuardarTareas()
        {
            using (StreamWriter escritor = new StreamWriter(rutaArchivo, false))
            {
                foreach (var tarea in listaTareas)
                {
                    escritor.WriteLine(tarea.ALinea());
                }
            }
        }

        static void CargarTareas()
        {
            if (!File.Exists(rutaArchivo)) return;

        }
    }
}