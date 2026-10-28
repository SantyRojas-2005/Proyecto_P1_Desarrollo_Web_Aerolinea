using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Progra_03.Models
{
    public static class ValidadorIdentificacion
    {
        public static bool ValidarCedulaEcuatoriana(string cedula)
        {
            // 1. Longitud exacta de 10 caracteres y solo números
            if (string.IsNullOrWhiteSpace(cedula) || cedula.Length != 10 || !cedula.All(char.IsDigit))
                return false;

            // 2. Validación de provincia (primeros dos dígitos entre 01 y 24)
            int provincia = int.Parse(cedula.Substring(0, 2));
            if (provincia < 1 || provincia > 24)
                return false;

            // 3. Validación del tercer dígito (menor a 6 para personas naturales)
            int tercerDigito = int.Parse(cedula.Substring(2, 1));
            if (tercerDigito >= 6)
                return false;

            // 4. Algoritmo Módulo 10
            int[] coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int suma = 0;

            for (int i = 0; i < 9; i++)
            {
                int valor = int.Parse(cedula[i].ToString()) * coeficientes[i];
                // CORRECCIÓN: Se agregó el operador de resta (valor - 9)
                suma += (valor >= 10) ? (valor - 9) : valor;
            }

            int digitoVerificador = int.Parse(cedula[9].ToString());
            int digitoCalculado = (suma % 10 == 0) ? 0 : 10 - (suma % 10);

            return digitoCalculado == digitoVerificador;
        }
    }
}