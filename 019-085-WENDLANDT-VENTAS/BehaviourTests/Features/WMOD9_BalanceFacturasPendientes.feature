Feature: WMOD9_BalanceFacturasPendientes
	Muestra balance y facturas pendientes con fechas de pago del crédito

@mytag
Scenario: Show balance and invoices with due date
	Given a sale status change
	When the sale status is changed to "Entregado"
	Then sale due date is set to today + credit days