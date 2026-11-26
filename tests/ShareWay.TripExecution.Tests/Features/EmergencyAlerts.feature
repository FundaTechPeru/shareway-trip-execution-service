# language: es
Característica: Alertas de emergencia
  Para que operaciones vea cualquier incidente que ocurra durante un viaje
  Como conductor o pasajero
  Quiero registrar una alerta y que un administrador la marque como atendida

  Escenario: El conductor registra una alerta y queda abierta
    Dada una alerta de emergencia reportada por el conductor con tipo "ACCIDENTE" y descripción "Choque en Panamericana Sur"
    Entonces la alerta queda en estado "Abierta"
    Y la alerta levanta un evento "AlertaEmergenciaRegistrada"

  Escenario: Un administrador atiende la alerta
    Dada una alerta de emergencia reportada por el conductor con tipo "ACCIDENTE" y descripción "Choque en Panamericana Sur"
    Cuando un administrador atiende la alerta
    Entonces la alerta queda en estado "Atendida"
    Y la alerta levanta un evento "AlertaEmergenciaAtendida"

  Escenario: No se puede atender una alerta dos veces
    Dada una alerta de emergencia reportada por el conductor con tipo "ACCIDENTE" y descripción "Choque en Panamericana Sur"
    Y la alerta ya fue atendida por un administrador
    Cuando se intenta atender la alerta nuevamente
    Entonces la operación falla con una regla de negocio
