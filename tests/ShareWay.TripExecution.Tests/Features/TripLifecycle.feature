# language: es
Característica: Ciclo de vida del viaje
  Para operar un viaje de principio a fin
  Como conductor
  Quiero crear, iniciar, finalizar y ajustar la ruta del viaje

  Escenario: Crear, iniciar y finalizar un viaje
    Dado un viaje confirmado con un pasajero cuyo código es "1234"
    Cuando el pasajero aborda con el código "1234"
    Y el conductor inicia el viaje
    Y el conductor finaliza el viaje
    Entonces el estado del viaje es "Finalizado"

  Escenario: Cancelar la parada de un pasajero recalcula la ruta sin cancelar el viaje
    Dado un viaje en curso con paradas para "Ana" y "Beto"
    Cuando se cancela la parada de "Ana" por motivo "el pasajero canceló su reserva"
    Entonces la ruta ya no contiene la parada de "Ana"
    Y la ruta todavía contiene la parada de "Beto"
    Y el estado del viaje es "EnCurso"
