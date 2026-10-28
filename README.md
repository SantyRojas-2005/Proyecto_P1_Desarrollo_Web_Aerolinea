# Sistema Web - Aerolínea VIP (Primera Entrega)

Proyecto para el desarrollo de un sistema web monolítico destinado a la consulta, reserva, compra simulada y administración de pasajes de una aerolínea VIP (Altévia). Desarrollado en ASP.NET Framework 4.8.1 con arquitectura MVC.

**Institución:** Universidad de las Fuerzas Armadas ESPE  
**Grupo 3:** Jhaldry Santiago Peñaherrera Rojas, Diego Xavier Pinto Segovia  

---

## 1. Requisitos Previos
* **Visual Studio 2019/2022** (con carga de trabajo de desarrollo web en ASP.NET y .NET Framework).
* **Microsoft SQL Server** (LocalDB, SQLEXPRESS o Developer).

## 2. Configuración de la Base de Datos
El proyecto utiliza dos bases de datos separadas (una para el dominio del negocio y otra para ASP.NET Identity). Para inicializarlas:
1. Abra Microsoft SQL Server Management Studio (SSMS).
2. Localice el archivo `Script_BD_AerolineaVIP_Unificado.sql` (ubicado en la carpeta `database/`).
3. Ejecute el script completo. Este archivo creará automáticamente las bases de datos `AerolineaVIP` y `AerolineaVIP_Identity`, junto con todas las tablas, vistas, y datos operativos de demostración de forma íntegra.

## 3. Ejecución del Proyecto
1. Abra el archivo de la solución `.sln` en Visual Studio.
2. Haga clic en **Compilar > Recompilar solución** para restaurar los paquetes NuGet y limpiar compilaciones previas.
3. Presione **F5** o haga clic en **Iniciar** (IIS Express) para ejecutar la aplicación en el navegador.

*(Nota: Los roles de seguridad y los usuarios base se generan automáticamente por código al arrancar la aplicación por primera vez).*

## 4. Credenciales de Prueba
El sistema inicializa automáticamente las siguientes cuentas con sus respectivos roles para que pueda evaluar todas las interfaces y niveles de seguridad:

| Perfil / Rol | Correo (Usuario) | Contraseña |
| :--- | :--- | :--- |
| **SuperUsuario** | `superadmin@altevia.com` | `Superadmin123*` |
| **Administrador** | `administrador@altevia.com` | `Administrador123*` |
| **Counter** | `counter@altevia.com` | `Counter123*` |
| **Cliente** | `cliente@altevia.com` | `Cliente123*` |

## 5. Notas sobre el Alcance de la Primera Entrega
* **Pagos Simulados:** La pasarela de pagos es una simulación académica (DP-06). No se realiza integración financiera real.
* **Comprobantes y PDF:** Los documentos y facturas generados (en pantalla y vía descarga PDF) son exclusivamente académicos, sin valor tributario (DP-05).
* **Flota y Capacidad:** La flota cuenta con aviones Embraer Phenom 300E con una estricta validación de capacidad máxima de 7 pasajeros por vuelo.
