$(function () {
    $(".forma-ajax--modal").ajaxForm(opcionesFormaModal);
    $(".forma-ajax").ajaxForm(opcionesForma);
    $(".forma-ajax--modal-refresh").ajaxForm(opcionesFormaModalOld);
});

var opcionesFormaModal = {
    beforeSubmit: prepararPeticion,
    success: ejecutarRespuesta,
    error: mostrarError
};

var opcionesFormaModalOld = {
    beforeSubmit: prepararPeticion,
    success: ejecutarRespuestaOld,
    error: mostrarError
};

var opcionesForma = {
    beforeSubmit: prepararPeticion,
    success: ejecutarRespuestaForma,
    error: mostrarError
};


function prepararPeticion(arr, $form, options) {
    // valida la forma, si pasa continua el submit
    $form.validate();
    var button = $form.find('button[type="submit"]');
    $form.find(".modal-errores").html("");

    if ($form.valid()) {
        monoloading('show');
        button.prop("disabled", true);
    } else {
        Console.log("error");
        $form.find(".modal-errores").html("Error");
        button.prop("disabled", false);
        return false;
    }
}

function ejecutarRespuesta(responseText, statusText, xhr, $form) {
    var json = responseText;
    var $modal = $form.parents(".modal");

    $modal.find('button[type="submit"]').prop("disabled", false);
    monoloading('hide');

    $modal.find(".modal-errores").html("");
    if (json.status === "ok") {
        $modal.modal('hide');
        monotoast(json.body);

        attemptRefreshGrids($form); 
    } else {
        $modal.find(".modal-errores").html(`<p>${json.body}</p>`);
    }
}

function attemptRefreshGrids($form) {
    var willRefreshGrids = $form.data('refresh-grids');

    if (willRefreshGrids == true) {
        refreshGrids();
    } else if (typeof $form.find('.datatable') !== 'undefined') {
        document.getElementsByClassName('datatable')[document.getElementsByClassName('datatable').length - 1].ej2_instances[0].refresh();
    }
}

function refreshGrids() {
    var gridCount = document.getElementsByClassName('datatable').length;

    for (var x = 0; x < gridCount; x++) {
        document.getElementsByClassName('datatable')[x].ej2_instances[0].refresh();
    }
}

function ejecutarRespuestaForma(responseText, statusText, xhr, $form) {
    var json = responseText;

    $form.find('button[type="submit"]').prop("disabled", false);
    monoloading('hide');

    $form.find(".modal-errores").html("");
    if (json.status === "ok") {
        $form.resetForm();
        attemptRefreshGrids($form); 

        monotoast(json.body);
    } else {
        $form.find(".modal-errores").html(`<p>${json.body}</p>`);
    }
}


function ejecutarRespuestaOld(responseText, statusText, xhr, $form) {
    var json = responseText;
    var $modal = $form.parents(".modal");

    $modal.find(".modal-errores").html("");
    if (json.status === "ok") {
        Cookies.set("consola-toast", json.body);
        window.location.reload();
    } else {
        monoloading('hide');
        $modal.find('button[type="submit"]').prop("disabled", false);
        $modal.find(".modal-errores").html(`<p>${json.body}</p>`);
    }
}

function mostrarError(xhr, statusText, errorThrown, $form) {
    $form.find('[type="submit"]').prop("disabled", false);
    monoloading('hide');
    if (xhr.status === 401) {
        $form.find(".modal-errores")
            .html("<p>Tu sesión caducó, por favor <a href='/' target='_blank'>inicia sesión</a> e intenta de nuevo</p>");
    } else if (xhr.status === 500) {
        $form.find(".modal-errores")
            .html("<p>Ocurrió un error en el servidor</p>");
    } else {
        $form.find(".modal-errores")
            .html("<p>Ocurrió un error inesperado</p>");
    }
}

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
var map;
var geocoder;
var marker;
var originalPosition;

function initializeMap() {
    originalPosition = new google.maps.LatLng(32.77428035254273, -115.51078715624999);
    geocoder = new google.maps.Geocoder();

    var mapOptions = {
        zoom: 8,
        center: originalPosition
    };

    map = new google.maps.Map(document.getElementById("map-canvas"),
        mapOptions);
}

function putMarker() {
    if (originalPosition === null) {
        originalPosition = new google.maps.LatLng(32.77428035254273, -115.51078715624999);
    }
    marker = new google.maps.Marker({
        position: originalPosition,
        map: map,
        animation: google.maps.Animation.DROP,
        draggable: true,
        title: "Move me to the right position"
    });

    marker.setMap(map);
}

function codeAddress(address) {
    geocoder.geocode({ 'address': address },
        function(results, status) {
            if (status === google.maps.GeocoderStatus.OK) {
                originalPosition = results[0].geometry.location;
                map.setZoom(15);
                map.setCenter(originalPosition);
                marker.setPosition(originalPosition);
                onCodeAddressSuccess();
                return true;
            } else {
                return false;
            }
        });
}

function onCodeAddressSuccess() {
    return true;
}
// Monobits Modal.  Versión: 1.3
// Estados e implementación
// última modificación: 15/jun/2018
// por Alejandro Quiñones Guzmán

$(document)
    .on("show.coreui.modal",
        ".modal",
        function () {
            var zIndex = 1040 + (10 * $(".modal:visible").length);
            $(this).css("z-index", zIndex);
            setTimeout(function () {
                    $(".modal-backdrop").not(".modal-stack").css("z-index", zIndex - 1).addClass("modal-stack");
                },
                0);
    });

$(document)
    .on("shown.coreui.modal",
        ".modal",
        function () {
            if ($(this).find('.select2') !== 'undefined') {
                $(this).find('.select2').each(function (i, obj) {
                    var $select = $(obj);
                    if (typeof $select.attr("placeholder") !== 'undefined') {
                        $select.select2({
                            placeholder: $select.attr('placeholder'),
                            allowClear: true
                        });
                    } else {
                        $select.select2();
                    }
                });
               
            }
        });

$(document)
    .on("hidden.coreui.modal",
        ".modal",
        function (event) {
            if ($(".modal").hasClass("show")) {
                $("body").addClass("modal-open");
            }
    });

$(document)
    .on("hide.coreui.modal",
        ".modal",
        function (event) {
            if ($(this).find('.datatable-server-side').length > 0) {
                $dataTables.pop();
            }
        });

// carga un modal por ajax y lo pone en el DOM, activa la forma si es que tiene
$(document).on("click",
    ".btn-modal-action",
    function (e) {
        e.preventDefault();
        var $btn = $(this);
        var modalId = $btn.attr("href");
        var $modal = $(modalId);
        var url = $modal.data("url");
        var $container = $modal;
        var id = $btn.data("id");

        if (typeof id !== "undefined") {
            url = url + "/" + id;
        }

        monoloading("show");

        $.get(url,
            function (data) {
                monoloading("hide");
                $container.html(data);              

                var $form = $modal.find("form");
                if (!$.isEmptyObject($form)) {
                    $.validator.unobtrusive.parse($form);
                }
                $container.find(".forma-ajax").ajaxForm(opcionesForma);
                $container.find(".forma-ajax--modal").ajaxForm(opcionesFormaModal);
                $container.find(".forma-ajax--modal-refresh").ajaxForm(opcionesFormaModalOld);

                // limpia el contenido cuando se cierra para evitar hacer referencia a elementos ocultos
                $modal.on('hidden.coreui.modal',
                    function() {
                        $container.html(null);
                    });

                $modal.modal("show");
            }).fail(mostrarErrorSnack);
    });

function mostrarErrorSnack() {
    monoloading('hide');
    monotoast("Ocurrió un error inesperado");
}
// Monobits Toast.  Versión: 1.2
// Snackbar con mensaje y animación
// última modificación: 21/oct/2015
// por Alejandro Quiñones Guzmán

// muestra el mensaje en un toast dependiendo del string guardado en la cookie.
// Guardar su valor por JS con Cookies.set('consola-toast', 'mensaje')
// o en C# con Response.Cookie["consola-toast"].Value = "mensaje"
$(document)
    .ready(function() {
        showMessageByResult();
    });

function showMessageByResult() {
    var msgCode = Cookies.get("consola-toast");

    if (typeof msgCode != "undefined") {
        if (msgCode != "") {
            showToast(msgCode);
        }
    }
}

function showToast(message) {
    monotoast(message);
    Cookies.remove("consola-toast");
}

function removeMessageCookie() {
    Cookies.remove("consola-toast");
}

// Función a llamar directamente
function monotoast(mensaje) {
    $("#alert").html(`<div class="mono-toast scene_element scene_element--fadeinup">${mensaje}</div>`);
    setTimeout(function() {
            $(".mono-toast")
                .animate({
                    bottom: -100,
                    opacity: 0
                });
        },
        3000);
}
// Monobits Utils.  Versión: 1.0
// Estados e implementación
// última modificación: 09/abril/2019
// por Alejandro Quiñones

// le da focus al primer input cuando se habre una colapsable
$(".collapse-focus-first").on("shown.coreui.collapse", function () {
    $(this).find('input:not([type="hidden"]):first').focus();
});

// enfoca el input del modal al abrir
$(".modal-focus-first").on("shown.coreui.modal", function () {
    $(this).find('input:not([type="hidden"]):first').focus();
});