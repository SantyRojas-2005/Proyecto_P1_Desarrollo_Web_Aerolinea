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
            if (string.IsNullOrWhiteSpace(cedula) || cedula.Length != 10 || !cedula.All(char.IsDigit))
                return false;
            int provincia = int.Parse(cedula.Substring(0, 2));
            if (provincia < 1 || provincia > 24)
                return false;
            int tercerDigito = int.Parse(cedula.Substring(2, 1));
            if (tercerDigito >= 6)
                return false;
            int[] coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int suma = 0;

            for (int i = 0; i < 9; i++)
            {
                int valor = int.Parse(cedula[i].ToString()) * coeficientes[i];
                suma += (valor >= 10) ? (valor - 9) : valor;
            }

            int digitoVerificador = int.Parse(cedula[9].ToString());
            int digitoCalculado = (suma % 10 == 0) ? 0 : 10 - (suma % 10);

            return digitoCalculado == digitoVerificador;
        }
    }
}
