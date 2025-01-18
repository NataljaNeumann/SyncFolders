
![SyncFolders-1](https://github.com/user-attachments/assets/3864175e-1b28-45eb-b56a-f95d1d338d44)  

[English](#en)[Français](#fr)[Español](#es)  

# English
<a name="en"></a>

SyncFolders is an application that aims to help you to keep two folders or drives synchronised.
With default settings this apllication creates hidden files, about 1% of the size of original files, that allow
you to completely recover from single block failures. There are two layers of protection:

1. You keep two different drives with complete copies of files.
2. Even if a drive becomes inaccessible, e.g. power failure or other problems, single block failures
and also bigger error ranges in files can be restored using additionally saved information.

The original files and additionally saved information can be verified by the application. In case
of errors the appllication will try to repair the file. If there is a second copy with same date and
same length, then app will try to recover single blocks from the other copy. If this fails then app
will try to recover from additionally saved information.

In case all mentioned measures fail, the application will also try to copy an older copy from the other
drive, which is the standard behavior of backup applications.

After all, if nothing worked, then the application will recover the available parts of the file by overwriting
unreadable blocks with zeros, so at least the file can be read and copied, even if not all parts of it are OK.
Many media applications can jump over these missing parts.

All of this is done automatically, so your personal family photos and videos are kept safe as good as possible.

A log is shown after completion of the operation and also saved in Documents folder for later reference.

There is no need of an installation. You can extract the archive into a subfolder of the drive that contains
photos and run it from there. Windows will eventually prompt you to install .NET-Framework, that is all you need.

If you choose to copy from first to second directory, then the app will treat the first directory as primary
data source and second directory as backup. If the application discovers that a file in first directory has
bad blocks, it still can try to restore the blocks from backup in second folder, or the older version of the
same file from the backup in second directory. You need to specify that first directory is not writable,
so the application doesn't try to modify the files in first directory.

The application usually runs in synchronization mode, which means that it will try to copy the newest version
of the photoes to the respective other folder or drive. If you remove the synchronization mode, then the application
can also overwrite new files in second directory by old files in first directory.

> [!NOTE]
If second directory contains special file, with the name specified below [^1], then the application
> doesn't delete the files in it, when you specify to delete the files in second directory.
# Français
<a name="fr"></a>
SyncFolders est une application qui vise à vous aider à garder deux dossiers ou lecteurs synchronisés. Avec les 
paramètres par défaut, cette application crée des fichiers cachés, environ 1 % de la taille des fichiers d'origine, 
qui vous permettent de récupérer complètement après des pannes d'un seul bloc. Il existe deux niveaux de protection :
1. Vous conservez deux lecteurs différents avec des copies complètes des fichiers.
2. Même si un lecteur devient inaccessible, par ex. une panne de courant ou d'autres problèmes, des pannes 
de bloc uniques ainsi que des plages d'erreurs plus importantes dans les fichiers peuvent être restaurées 
à l'aide d'informations enregistrées supplémentaires.

Les fichiers originaux et les informations supplémentaires enregistrées peuvent être vérifiés par l'application. 
En cas d'erreurs, l'application tentera de réparer le fichier. S'il existe une deuxième copie avec la même date et 
la même longueur, l'application tentera de récupérer des blocs uniques de l'autre copie. Si cela échoue, 
l'application tentera de récupérer des informations supplémentaires enregistrées.

En cas d'échec de toutes les mesures mentionnées, l'application tentera également de copier une copie plus ancienne 
de l'autre lecteur, ce qui constitue le comportement standard des applications de sauvegarde.

Après tout, si rien ne fonctionne, l'application récupérera les parties disponibles du fichier en écrasant les 
blocs illisibles par des zéros, afin qu'au moins le fichier puisse être lu et copié, même si toutes les parties 
ne sont pas correctes. De nombreuses applications multimédias peuvent combler ces lacunes.

Tout cela se fait automatiquement, afin que vos photos et vidéos personnelles de famille soient protégées autant que possible.

Un journal est affiché une fois l'opération terminée et également enregistré dans le dossier Documents pour référence ultérieure.

l n'y a pas besoin d'installation. Vous pouvez extraire l'archive dans un sous-dossier du lecteur contenant des photos 
et l'exécuter à partir de là. Windows vous demandera éventuellement d'installer .NET-Framework, c'est tout ce dont vous avez besoin.

Si vous choisissez de copier du premier vers le deuxième répertoire, l'application traitera le premier répertoire comme 
source de données principale et le deuxième répertoire comme sauvegarde. Si l'application découvre qu'un fichier du premier 
répertoire contient des blocs défectueux, elle peut toujours essayer de restaurer les blocs de la sauvegarde dans le 
deuxième dossier, ou l'ancienne version du même fichier à partir de la sauvegarde dans le deuxième répertoire. 
Vous devez spécifier que le premier répertoire n'est pas accessible en écriture, afin que 
l'application n'essaye pas de modifier les fichiers du premier répertoire.

L'application fonctionne généralement en mode synchronisation, ce qui signifie qu'elle essaiera de copier 
la version la plus récente des photos dans l'autre dossier ou lecteur correspondant. Si vous supprimez le mode 
de synchronisation, l'application peut également écraser les nouveaux fichiers du deuxième répertoire par les 
anciens fichiers du premier répertoire. L'application s'exécute généralement en mode de synchronisation, 
ce qui signifie qu'elle essaiera de copier la version la plus récente des photos dans le répertoire respectif. 
autre dossier ou lecteur. Si vous supprimez le mode de synchronisation, l'application peut également remplacer 
les nouveaux fichiers du deuxième répertoire par les anciens fichiers du premier répertoire.

> [!NOTE]
Si le deuxième répertoire contient un fichier spécial, dont le nom est spécifié ci-dessous [^1],
> l'application ne supprime pas les fichiers qu'il contient lorsque vous spécifiez de supprimer les fichiers du deuxième répertoire.
# Español
<a name="es"></a>
SyncFolders es una aplicación que tiene como objetivo ayudarte a mantener sincronizadas dos carpetas o unidades.
Con la configuración predeterminada, esta aplicación crea archivos ocultos, aproximadamente el 1% del tamaño de 
los archivos originales, que permiten para recuperarse completamente de los errores de un solo bloque. 
Hay dos capas de protección:
- Conserva dos unidades diferentes con copias completas de los archivos.
- Incluso si una unidad se vuelve inaccesible, por ejemplo, corte de energía u otros problemas, 
fallas de un solo bloque Y también se pueden restaurar rangos de error más grandes en los archivos
utilizando información guardada adicionalmente.

La aplicación puede verificar los archivos originales y la información guardada adicionalmente. 
Por si de errores, la aplicación intentará reparar el archivo. Si hay una segunda copia con la 
misma fecha y misma longitud, luego la aplicación intentará recuperar bloques individuales de la 
otra copia. Si esto falla, entonces la aplicación intentará recuperarse de la información guardada adicionalmente.

En caso de que todas las medidas mencionadas fallen, la aplicación también intentará copiar una 
copia anterior de la otra unidad, que es el comportamiento estándar de las aplicaciones de copia de seguridad.

Después de todo, si nada funcionó, la aplicación recuperará las partes disponibles del archivo 
sobrescribiendo bloques ilegibles con ceros, por lo que al menos el archivo se puede leer y copiar, 
incluso si no todas las partes del mismo están bien. Muchas aplicaciones multimedia pueden saltar 
por encima de estas partes faltantes.

Todo esto se hace automáticamente, por lo que sus fotos y videos familiares personales se mantienen seguros lo mejor posible.

Se muestra un registro después de completar la operación y también se guarda en la carpeta Documentos para referencia posterior.

No hay necesidad de instalación. Puede extraer el archivo en una subcarpeta de la unidad que contiene 
fotos y ejecutarlo desde allí. Eventualmente, Windows le pedirá que instale . NET-Framework, eso es todo lo que necesitas.

Si elige copiar del primer al segundo directorio, la aplicación tratará el primer directorio como 
principal fuente de datos y segundo directorio como copia de seguridad. Si la aplicación detecta que 
un archivo en el primer directorio tiene bloques defectuosos, todavía puede intentar restaurar los bloques 
desde la copia de seguridad en la segunda carpeta, o la versión anterior de la mismo archivo de la copia 
de seguridad en el segundo directorio. Debe especificar que el primer directorio no se puede escribir, 
Por lo tanto, la aplicación no intenta modificar los archivos en el primer directorio.

La aplicación generalmente se ejecuta en modo de sincronización, lo que significa que intentará copiar 
la versión más reciente de las fotos a la otra carpeta o unidad respectiva. Si quita el modo de sincronización, 
la aplicación También puede sobrescribir nuevos archivos en el segundo directorio por archivos antiguos en el primer directorio.

> [!NOTE]
Si el segundo directorio contiene un archivo especial, con el nombre especificado a continuación [^1], 
> entonces la aplicación No elimina los archivos que contiene, cuando se especifica eliminar los archivos en el segundo directorio.


[^1] SyncFolders-Don't-Delete.txt

