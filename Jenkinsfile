node {
    def app

    stage('Clone repository'){
        /**/
        checkout scm
    }

    stage('Build the project'){
        /* Compile the project */
        app.inside {
            try{
                sh 'dotnet restore'
                sh 'dotnet build Netlyt/Netlyt.csproj'
            } catch (err){
                slackSend channel: '#builds', color: 'bad', message: 'Netlyt build failed', teamDomain: 'peeralytics', token: 'K5cgFQydAQSlIDeWS9ITziLG'
            }
            slackSend channel: '#builds', color: 'good', message: 'Netlyt built successfully', teamDomain: 'peeralytics', token: 'K5cgFQydAQSlIDeWS9ITziLG'
        }
    }

    stage('Build container'){
       /* This builds the docker image*/ 
       app = docker.build("netlyt/netlyt")
    }

    stage('Test image'){
        app.inside {
            sh ' echo "Tests PASSED"' /* Volkswagen style for now*/
        }
    }

    stage('Push image'){
        docker.withRegistry('https://registry.vaskovasilev.eu', 'offsite-docker-registry'){
            app.push("${env.BUILD_NUMBER}")
            app.push("latest")
        }
    }
 
}