FROM nginx:alpine

COPY index.html app.js config.json /usr/share/nginx/html/

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
