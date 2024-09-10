// Monobits Loading.  Versión: 1.2
// Estados e implementación
// última modificación: 21/oct/2015
// por Alejandro Quiñones Guzmán

// Contenedor de Loading, el icono puede ser cambiado
// se requiere una etiqueta con el ID #alertTop en la raiz del <body>
function monomiddle(icono) {
    $("#alertTop")
        .html(`<div class="mono-middle scene_element scene_element--fadeindown shadow-z-1">${icono}</div>`);
}

// Función a llamar directamente
// Estados:
// 'show' hace aparecer el círculo de cargando
// 'hide' oculta el círculo de cargando
function monoloading(estado) {
    if (estado === "show") {
        monomiddle('<i class="fa fa-2x fa-spinner fa-pulse"></i>');
    } else if (estado === "hide") {
        $(".mono-middle")
            .animate({
                top: -100,
                opacity: 0
            });
    }
}