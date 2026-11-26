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

  Escenario: Cancelar la última parada deja el viaje sin paradas pero sigue en curso
    Dado un viaje en curso con paradas para "Ana" y "Beto"
    Cuando se cancela la parada de "Ana" por motivo "no asiste"
    Y se cancela la parada de "Beto" por motivo "no asiste"
    Entonces la ruta ya no contiene la parada de "Ana"
    Y la ruta ya no contiene la parada de "Beto"
    Y el estado del viaje es "EnCurso"

  Escenario: Cancelar una parada en un viaje finalizado es rechazado
    Dado un viaje en curso con paradas para "Ana" y "Beto"
    Y el conductor finaliza el viaje
    Cuando se intenta cancelar la parada de "Ana" por motivo "fuera de tiempo"
    Entonces la operación falla con una transición inválida

  Escenario: Cancelar una parada inexistente es rechazado
    Dado un viaje en curso con paradas para "Ana" y "Beto"
    Cuando se intenta cancelar una parada inexistente
    Entonces la operación falla porque el pasajero no pertenece al viaje
