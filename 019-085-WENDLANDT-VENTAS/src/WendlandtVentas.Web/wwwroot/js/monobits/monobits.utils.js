// Monobits Utils.  Versión: 1.0
// Estados e implementación
// última modificación: 09/abril/2019
// por Alejandro Quiñones

// le da focus al primer input cuando se habre una colapsable
$(".collapse-focus-first").on("shown.bs.collapse", function () {
    $(this).find('input:not([type="hidden"]):first').focus();
});

// enfoca el input del modal al abrir
$(".modal-focus-first").on("shown.bs.modal", function () {
    $(this).find('input:not([type="hidden"]):first').focus();
});